//MIT, 2015, Michael Popoloski's SharpFont
using System;
using System.Numerics;

namespace Typography.OpenFont
{


    public class TrueTypeInterpreter
    {
        Typeface _currentTypeFace;
        SharpFontInterpreter _interpreter;
        public Typeface Typeface
        {
            get => _currentTypeFace;
            set => SetTypeFace(value);
        }

        public void SetTypeFace(Typeface typeface)
        {
            //still preserve this for compat with others,
            //wait for other libs...

            _currentTypeFace = typeface;
            Tables.MaxProfile maximumProfile = _currentTypeFace.MaxProfile;
            _interpreter = new SharpFontInterpreter(
                maximumProfile.MaxStackElements,
                maximumProfile.MaxStorage,
                maximumProfile.MaxFunctionDefs,
                maximumProfile.MaxInstructionDefs,
                maximumProfile.MaxTwilightPoints);
            // the fpgm table optionally contains a program to run at initialization time 
            if (_currentTypeFace.FpgmProgramBuffer != null)
            {
                _interpreter.InitializeFunctionDefs(_currentTypeFace.FpgmProgramBuffer);
            }
        }


        public GlyphPointF[] HintGlyph(ushort glyphIndex, float glyphSizeInPixel)
        {

            Glyph glyph = _currentTypeFace.GetGlyph(glyphIndex);
            //-------------------------------------------
            //1. start with original points/contours from glyph 
            int horizontalAdv = _currentTypeFace.GetAdvanceWidthFromGlyphIndex(glyphIndex);
            int hFrontSideBearing = _currentTypeFace.GetLeftSideBearing(glyphIndex);

            return HintGlyph(horizontalAdv,
                hFrontSideBearing,
                glyph.MinX,
                glyph.MaxY,
                glyph.GlyphPoints,
                glyph.EndPoints,
                glyph.GlyphInstructions,
                glyphSizeInPixel);

        }
        public GlyphPointF[] HintGlyph(
            int horizontalAdv,
            int hFrontSideBearing,
            int minX,
            int maxY,
            GlyphPointF[] glyphPoints,
            ushort[] contourEndPoints,
            byte[] instructions,
            float glyphSizeInPixel)
        {

            //get glyph for its matrix

            //TODO: review here again

            int verticalAdv = 0;
            int vFrontSideBearing = 0;
            var pp1 = new GlyphPointF((minX - hFrontSideBearing), 0, true);
            var pp2 = new GlyphPointF(pp1.X + horizontalAdv, 0, true);
            var pp3 = new GlyphPointF(0, maxY + vFrontSideBearing, true);
            var pp4 = new GlyphPointF(0, pp3.Y - verticalAdv, true);
            //-------------------------

            //2. use a clone version extend org with 4 elems
            int orgLen = glyphPoints.Length;
            GlyphPointF[] newGlyphPoints = Utils.CloneArray(glyphPoints, 4);
            // add phantom points; these are used to define the extents of the glyph,
            // and can be modified by hinting instructions
            newGlyphPoints[orgLen] = pp1;
            newGlyphPoints[orgLen + 1] = pp2;
            newGlyphPoints[orgLen + 2] = pp3;
            newGlyphPoints[orgLen + 3] = pp4;

            //3. scale all point to target pixel size
            float pxScale = _currentTypeFace.CalculateScaleToPixel(glyphSizeInPixel);
            for (int i = orgLen + 3; i >= 0; --i)
            {
                newGlyphPoints[i].ApplyScale(pxScale);
            }

            //----------------------------------------------
            //test : agg's vertical hint
            //apply large scale on horizontal axis only 
            //translate and then scale back
            float agg_x_scale = 1000;
            //
            if (UseVerticalHinting)
            {
                ApplyScaleOnlyOnXAxis(newGlyphPoints, agg_x_scale);
            }

            //4.  
            _interpreter.SetControlValueTable(_currentTypeFace.ControlValues,
                pxScale,
                glyphSizeInPixel,
                _currentTypeFace.PrepProgramBuffer);
            //--------------------------------------------------
            //5. hint
            _interpreter.HintGlyph(newGlyphPoints, contourEndPoints, instructions);

            //6. scale back
            if (UseVerticalHinting)
            {
                ApplyScaleOnlyOnXAxis(newGlyphPoints, 1f / agg_x_scale);
            }
            return newGlyphPoints;

        }

        public bool UseVerticalHinting { get; set; }

        static void ApplyScaleOnlyOnXAxis(GlyphPointF[] glyphPoints, float xscale)
        {
            //TODO: review performance here
            for (int i = glyphPoints.Length - 1; i >= 0; --i)
            {
                glyphPoints[i].ApplyScaleOnlyOnXAxis(xscale);
            }

        }

    }


    /// <summary>
    /// SharpFont's TrueType Interpreter
    /// </summary>
    class SharpFontInterpreter
    {
        GraphicsState _state;
        GraphicsState _cvtState;
        ExecutionStack _stack;
        InstructionStream[] _functions;
        InstructionStream[] _instructionDefs;
        float[] _controlValueTable;
        int[] _storage;
        ushort[] _contours;
        float _scale;
        int _ppem;
        int _callStackSize;
        float _fdotp;
        float _roundThreshold;
        float _roundPhase;
        float roundPeriod;
        Zone _zp0, _zp1, _zp2;
        Zone _points, _twilight;

        public SharpFontInterpreter(int maxStack, int maxStorage, int maxFunctions, int maxInstructionDefs, int maxTwilightPoints)
        {
            _stack = new ExecutionStack(maxStack);
            _storage = new int[maxStorage];
            _functions = new InstructionStream[maxFunctions];
            _instructionDefs = new InstructionStream[maxInstructionDefs > 0 ? 256 : 0];
            _state = new GraphicsState();
            _cvtState = new GraphicsState();
            _twilight = new Zone(new GlyphPointF[maxTwilightPoints], isTwilight: true);
        }

        public void InitializeFunctionDefs(byte[] instructions)
        {
            Execute(new InstructionStream(instructions), false, true);
        }

        public void SetControlValueTable(int[] cvt, float scale, float ppem, byte[] cvProgram)
        {
            if (_scale == scale || cvt == null)
                return;

            if (_controlValueTable == null)
                _controlValueTable = new float[cvt.Length];
            //copy cvt and apply scale
            for (int i = cvt.Length - 1; i >= 0; --i)
                _controlValueTable[i] = cvt[i] * scale;

            _scale = scale;
            _ppem = (int)Math.Round(ppem);
            _zp0 = _zp1 = _zp2 = _points;
            _state.Reset();
            _stack.Clear();

            if (cvProgram != null)
            {
                Execute(new InstructionStream(cvProgram), false, false);

                // save off the CVT graphics state so that we can restore it for each glyph we hint
                if ((_state.InstructionControl & InstructionControlFlags.UseDefaultGraphicsState) != 0)
                    _cvtState.Reset();
                else
                {
                    // always reset a few fields; copy the reset
                    _cvtState = _state;
                    _cvtState.Freedom = Vector2.UnitX;
                    _cvtState.Projection = Vector2.UnitX;
                    _cvtState.DualProjection = Vector2.UnitX;
                    _cvtState.RoundState = RoundMode.ToGrid;
                    _cvtState.Loop = 1;
                }
            }
        }

        public void HintGlyph(GlyphPointF[] glyphPoints, ushort[] contours, byte[] instructions)
        {
            if (instructions == null || instructions.Length == 0)
                return;

            // check if the CVT program disabled hinting
            if ((_state.InstructionControl & InstructionControlFlags.InhibitGridFitting) != 0)
                return;

            // TODO: composite glyphs
            // TODO: round the phantom points?

            // save contours and points
            _contours = contours;
            _zp0 = _zp1 = _zp2 = _points = new Zone(glyphPoints, isTwilight: false);

            // reset all of our shared state
            _state = _cvtState;
            _callStackSize = 0;
#if DEBUG
            debugList.Clear();
#endif
            _stack.Clear();
            OnVectorsUpdated();

            // normalize the round state settings
            switch (_state.RoundState)
            {
                case RoundMode.Super: SetSuperRound(1.0f); break;
                case RoundMode.Super45: SetSuperRound(Sqrt2Over2); break;
            }

            try
            {
                Execute(new InstructionStream(instructions), false, false);
            }
            catch (InvalidTrueTypeFontException)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine("invalid_font_ex:");
#endif

            }
        }

#if DEBUG
        System.Collections.Generic.List<OpCode> debugList = new System.Collections.Generic.List<OpCode>();
#endif
        void Execute(InstructionStream stream, bool inFunction, bool allowFunctionDefs)
        {
            // dispatch each instruction in the stream
            while (!stream.Done)
            {
                var opcode = stream.NextOpCode();
#if DEBUG
                debugList.Add(opcode);
#endif
                switch (opcode)
                {
                    // ==== PUSH INSTRUCTIONS ====
                    case OpCode.NPUSHB:
                    case OpCode.PUSHB1:
                    case OpCode.PUSHB2:
                    case OpCode.PUSHB3:
                    case OpCode.PUSHB4:
                    case OpCode.PUSHB5:
                    case OpCode.PUSHB6:
                    case OpCode.PUSHB7:
                    case OpCode.PUSHB8:
                        {
                            var count = opcode == OpCode.NPUSHB ? stream.NextByte() : opcode - OpCode.PUSHB1 + 1;
                            for (int i = count - 1; i >= 0; --i)
                                _stack.Push(stream.NextByte());
                        }
                        break;
                    case OpCode.NPUSHW:
                    case OpCode.PUSHW1:
                    case OpCode.PUSHW2:
                    case OpCode.PUSHW3:
                    case OpCode.PUSHW4:
                    case OpCode.PUSHW5:
                    case OpCode.PUSHW6:
                    case OpCode.PUSHW7:
                    case OpCode.PUSHW8:
                        {
                            var count = opcode == OpCode.NPUSHW ? stream.NextByte() : opcode - OpCode.PUSHW1 + 1;
                            for (int i = count - 1; i >= 0; --i)
                                _stack.Push(stream.NextWord());
                        }
                        break;

                    // ==== STORAGE MANAGEMENT ====
                    case OpCode.RS:
                        {
                            var loc = CheckIndex(_stack.Pop(), _storage.Length);
                            _stack.Push(_storage[loc]);
                        }
                        break;
                    case OpCode.WS:
                        {
                            var value = _stack.Pop();
                            var loc = CheckIndex(_stack.Pop(), _storage.Length);
                            _storage[loc] = value;
                        }
                        break;

                    // ==== CONTROL VALUE TABLE ====
                    case OpCode.WCVTP:
                        {
                            var value = _stack.PopFloat();
                            var loc = CheckIndex(_stack.Pop(), _controlValueTable.Length);
                            _controlValueTable[loc] = value;
                        }
                        break;
                    case OpCode.WCVTF:
                        {
                            var value = _stack.Pop();
                            var loc = CheckIndex(_stack.Pop(), _controlValueTable.Length);
                            _controlValueTable[loc] = value * _scale;
                        }
                        break;
                    case OpCode.RCVT: _stack.Push(ReadCvt()); break;

                    // ==== STATE VECTORS ====
                    case OpCode.SVTCA0:
                    case OpCode.SVTCA1:
                        {
                            var axis = opcode - OpCode.SVTCA0;
                            SetFreedomVectorToAxis(axis);
                            SetProjectionVectorToAxis(axis);
                        }
                        break;
                    case OpCode.SFVTPV: _state.Freedom = _state.Projection; OnVectorsUpdated(); break;
                    case OpCode.SPVTCA0:
                    case OpCode.SPVTCA1: SetProjectionVectorToAxis(opcode - OpCode.SPVTCA0); break;
                    case OpCode.SFVTCA0:
                    case OpCode.SFVTCA1: SetFreedomVectorToAxis(opcode - OpCode.SFVTCA0); break;
                    case OpCode.SPVTL0:
                    case OpCode.SPVTL1:
                    case OpCode.SFVTL0:
                    case OpCode.SFVTL1: SetVectorToLine(opcode - OpCode.SPVTL0, false); break;
                    case OpCode.SDPVTL0:
                    case OpCode.SDPVTL1: SetVectorToLine(opcode - OpCode.SDPVTL0, true); break;
                    case OpCode.SPVFS:
                    case OpCode.SFVFS:
                        {
                            var y = _stack.Pop();
                            var x = _stack.Pop();
                            var vec = Vector2.Normalize(new Vector2(F2Dot14ToFloat(x), F2Dot14ToFloat(y)));
                            if (opcode == OpCode.SFVFS)
                                _state.Freedom = vec;
                            else
                            {
                                _state.Projection = vec;
                                _state.DualProjection = vec;
                            }
                            OnVectorsUpdated();
                        }
                        break;
                    case OpCode.GPV:
                    case OpCode.GFV:
                        {
                            var vec = opcode == OpCode.GPV ? _state.Projection : _state.Freedom;
                            _stack.Push(FloatToF2Dot14(vec.X));
                            _stack.Push(FloatToF2Dot14(vec.Y));
                        }
                        break;

                    // ==== GRAPHICS STATE ====
                    case OpCode.SRP0: _state.Rp0 = _stack.Pop(); break;
                    case OpCode.SRP1: _state.Rp1 = _stack.Pop(); break;
                    case OpCode.SRP2: _state.Rp2 = _stack.Pop(); break;
                    case OpCode.SZP0: _zp0 = GetZoneFromStack(); break;
                    case OpCode.SZP1: _zp1 = GetZoneFromStack(); break;
                    case OpCode.SZP2: _zp2 = GetZoneFromStack(); break;
                    case OpCode.SZPS: _zp0 = _zp1 = _zp2 = GetZoneFromStack(); break;
                    case OpCode.RTHG: _state.RoundState = RoundMode.ToHalfGrid; break;
                    case OpCode.RTG: _state.RoundState = RoundMode.ToGrid; break;
                    case OpCode.RTDG: _state.RoundState = RoundMode.ToDoubleGrid; break;
                    case OpCode.RDTG: _state.RoundState = RoundMode.DownToGrid; break;
                    case OpCode.RUTG: _state.RoundState = RoundMode.UpToGrid; break;
                    case OpCode.ROFF: _state.RoundState = RoundMode.Off; break;
                    case OpCode.SROUND: _state.RoundState = RoundMode.Super; SetSuperRound(1.0f); break;
                    case OpCode.S45ROUND: _state.RoundState = RoundMode.Super45; SetSuperRound(Sqrt2Over2); break;
                    case OpCode.INSTCTRL:
                        {
                            var selector = _stack.Pop();
                            if (selector >= 1 && selector <= 2)
                            {
                                // value is false if zero, otherwise shift the right bit into the flags
                                var bit = 1 << (selector - 1);
                                if (_stack.Pop() == 0)
                                    _state.InstructionControl = (InstructionControlFlags)((int)_state.InstructionControl & ~bit);
                                else
                                    _state.InstructionControl = (InstructionControlFlags)((int)_state.InstructionControl | bit);
                            }
                        }
                        break;
                    case OpCode.SCANCTRL: /* instruction unspported */ _stack.Pop(); break;
                    case OpCode.SCANTYPE: /* instruction unspported */ _stack.Pop(); break;
                    case OpCode.SANGW: /* instruction unspported */ _stack.Pop(); break;
                    case OpCode.SLOOP: _state.Loop = _stack.Pop(); break;
                    case OpCode.SMD: _state.MinDistance = _stack.PopFloat(); break;
                    case OpCode.SCVTCI: _state.ControlValueCutIn = _stack.PopFloat(); break;
                    case OpCode.SSWCI: _state.SingleWidthCutIn = _stack.PopFloat(); break;
                    case OpCode.SSW: _state.SingleWidthValue = _stack.Pop() * _scale; break;
                    case OpCode.FLIPON: _state.AutoFlip = true; break;
                    case OpCode.FLIPOFF: _state.AutoFlip = false; break;
                    case OpCode.SDB: _state.DeltaBase = _stack.Pop(); break;
                    case OpCode.SDS: _state.DeltaShift = _stack.Pop(); break;

                    // ==== POINT MEASUREMENT ====
                    case OpCode.GC0: _stack.Push(Project(_zp2.GetCurrent(_stack.Pop()))); break;
                    case OpCode.GC1: _stack.Push(DualProject(_zp2.GetOriginal(_stack.Pop()))); break;
                    case OpCode.SCFS:
                        {
                            var value = _stack.PopFloat();
                            var index = _stack.Pop();
                            var point = _zp2.GetCurrent(index);
                            MovePoint(_zp2, index, value - Project(point));

                            // moving twilight points moves their "original" value also
                            if (_zp2.IsTwilight)
                                _zp2.Original[index].P = _zp2.Current[index].P;
                        }
                        break;
                    case OpCode.MD0:
                        {
                            var p1 = _zp1.GetOriginal(_stack.Pop());
                            var p2 = _zp0.GetOriginal(_stack.Pop());
                            _stack.Push(DualProject(p2 - p1));
                        }
                        break;
                    case OpCode.MD1:
                        {
                            var p1 = _zp1.GetCurrent(_stack.Pop());
                            var p2 = _zp0.GetCurrent(_stack.Pop());
                            _stack.Push(Project(p2 - p1));
                        }
                        break;
                    case OpCode.MPS: // MPS should return point size, but we assume DPI so it's the same as pixel size
                    case OpCode.MPPEM: _stack.Push(_ppem); break;
                    case OpCode.AA: /* deprecated instruction */ _stack.Pop(); break;

                    // ==== POINT MODIFICATION ====
                    case OpCode.FLIPPT:
                        {
                            for (int i = 0; i < _state.Loop; i++)
                            {
                                var index = _stack.Pop();
                                //review here again!
                                _points.Current[index].onCurve = !_points.Current[index].onCurve;
                                //if (points.Current[index].onCurve)
                                //    points.Current[index].onCurve = false;
                                //else
                                //    points.Current[index].onCurve = true;
                            }
                            _state.Loop = 1;
                        }
                        break;
                    case OpCode.FLIPRGON:
                        {
                            var end = _stack.Pop();
                            for (int i = _stack.Pop(); i <= end; i++)
                                //points.Current[i].Type = PointType.OnCurve;
                                _points.Current[i].onCurve = true;
                        }
                        break;
                    case OpCode.FLIPRGOFF:
                        {
                            var end = _stack.Pop();
                            for (int i = _stack.Pop(); i <= end; i++)
                                //points.Current[i].Type = PointType.Quadratic;
                                _points.Current[i].onCurve = false;
                        }
                        break;
                    case OpCode.SHP0:
                    case OpCode.SHP1:
                        {
                            Zone zone;
                            int point;
                            var displacement = ComputeDisplacement((int)opcode, out zone, out point);
                            ShiftPoints(displacement);
                        }
                        break;
                    case OpCode.SHPIX: ShiftPoints(_stack.PopFloat() * _state.Freedom); break;
                    case OpCode.SHC0:
                    case OpCode.SHC1:
                        {
                            Zone zone;
                            int point;
                            var displacement = ComputeDisplacement((int)opcode, out zone, out point);
                            var touch = GetTouchState();
                            var contour = _stack.Pop();
                            var start = contour == 0 ? 0 : _contours[contour - 1] + 1;
                            var count = _zp2.IsTwilight ? _zp2.Current.Length : _contours[contour] + 1;

                            for (int i = start; i < count; i++)
                            {
                                // don't move the reference point
                                if (zone.Current != _zp2.Current || point != i)
                                {
                                    _zp2.Current[i].P += displacement;
                                    _zp2.TouchState[i] |= touch;
                                }
                            }
                        }
                        break;
                    case OpCode.SHZ0:
                    case OpCode.SHZ1:
                        {
                            Zone zone;
                            int point;
                            var displacement = ComputeDisplacement((int)opcode, out zone, out point);
                            var count = 0;
                            if (_zp2.IsTwilight)
                                count = _zp2.Current.Length;
                            else if (_contours.Length > 0)
                                count = _contours[_contours.Length - 1] + 1;

                            for (int i = 0; i < count; i++)
                            {
                                // don't move the reference point
                                if (zone.Current != _zp2.Current || point != i)
                                    _zp2.Current[i].P += displacement;
                            }
                        }
                        break;
                    case OpCode.MIAP0:
                    case OpCode.MIAP1:
                        {
                            var distance = ReadCvt();
                            var pointIndex = _stack.Pop();

                            // this instruction is used in the CVT to set up twilight points with original values
                            if (_zp0.IsTwilight)
                            {
                                var original = _state.Freedom * distance;
                                _zp0.Original[pointIndex].P = original;
                                _zp0.Current[pointIndex].P = original;
                            }

                            // current position of the point along the projection vector
                            var point = _zp0.GetCurrent(pointIndex);
                            var currentPos = Project(point);
                            if (opcode == OpCode.MIAP1)
                            {
                                // only use the CVT if we are above the cut-in point
                                if (Math.Abs(distance - currentPos) > _state.ControlValueCutIn)
                                    distance = currentPos;
                                distance = Round(distance);
                            }

                            MovePoint(_zp0, pointIndex, distance - currentPos);
                            _state.Rp0 = pointIndex;
                            _state.Rp1 = pointIndex;
                        }
                        break;
                    case OpCode.MDAP0:
                    case OpCode.MDAP1:
                        {
                            var pointIndex = _stack.Pop();
                            var point = _zp0.GetCurrent(pointIndex);
                            var distance = 0.0f;
                            if (opcode == OpCode.MDAP1)
                            {
                                distance = Project(point);
                                distance = Round(distance) - distance;
                            }

                            MovePoint(_zp0, pointIndex, distance);
                            _state.Rp0 = pointIndex;
                            _state.Rp1 = pointIndex;
                        }
                        break;
                    case OpCode.MSIRP0:
                    case OpCode.MSIRP1:
                        {
                            var targetDistance = _stack.PopFloat();
                            var pointIndex = _stack.Pop();

                            // if we're operating on the twilight zone, initialize the points
                            if (_zp1.IsTwilight)
                            {
                                _zp1.Original[pointIndex].P = _zp0.Original[_state.Rp0].P + targetDistance * _state.Freedom / _fdotp;
                                _zp1.Current[pointIndex].P = _zp1.Original[pointIndex].P;
                            }

                            var currentDistance = Project(_zp1.GetCurrent(pointIndex) - _zp0.GetCurrent(_state.Rp0));
                            MovePoint(_zp1, pointIndex, targetDistance - currentDistance);

                            _state.Rp1 = _state.Rp0;
                            _state.Rp2 = pointIndex;
                            if (opcode == OpCode.MSIRP1)
                                _state.Rp0 = pointIndex;
                        }
                        break;
                    case OpCode.IP:
                        {
                            var originalBase = _zp0.GetOriginal(_state.Rp1);
                            var currentBase = _zp0.GetCurrent(_state.Rp1);
                            var originalRange = DualProject(_zp1.GetOriginal(_state.Rp2) - originalBase);
                            var currentRange = Project(_zp1.GetCurrent(_state.Rp2) - currentBase);

                            for (int i = 0; i < _state.Loop; i++)
                            {
                                var pointIndex = _stack.Pop();
                                var point = _zp2.GetCurrent(pointIndex);
                                var currentDistance = Project(point - currentBase);
                                var originalDistance = DualProject(_zp2.GetOriginal(pointIndex) - originalBase);

                                var newDistance = 0.0f;
                                if (originalDistance != 0.0f)
                                {
                                    // a range of 0.0f is invalid according to the spec (would result in a div by zero)
                                    if (originalRange == 0.0f)
                                        newDistance = originalDistance;
                                    else
                                        newDistance = originalDistance * currentRange / originalRange;
                                }

                                MovePoint(_zp2, pointIndex, newDistance - currentDistance);
                            }
                            _state.Loop = 1;
                        }
                        break;
                    case OpCode.ALIGNRP:
                        {
                            for (int i = 0; i < _state.Loop; i++)
                            {
                                var pointIndex = _stack.Pop();
                                var p1 = _zp1.GetCurrent(pointIndex);
                                var p2 = _zp0.GetCurrent(_state.Rp0);
                                MovePoint(_zp1, pointIndex, -Project(p1 - p2));
                            }
                            _state.Loop = 1;
                        }
                        break;
                    case OpCode.ALIGNPTS:
                        {
                            var p1 = _stack.Pop();
                            var p2 = _stack.Pop();
                            var distance = Project(_zp0.GetCurrent(p2) - _zp1.GetCurrent(p1)) / 2;
                            MovePoint(_zp1, p1, distance);
                            MovePoint(_zp0, p2, -distance);
                        }
                        break;
                    case OpCode.UTP: _zp0.TouchState[_stack.Pop()] &= ~GetTouchState(); break;
                    case OpCode.IUP0:
                    case OpCode.IUP1:
                        // bail if no contours (empty outline)
                        if (_contours.Length == 0)
                        {
                            break;
                        }

                        //{

                        //    //WinterDev's new managed version
                        //    GlyphPointF[] currentPnts = points.Current;
                        //    GlyphPointF[] originalPnts = points.Original;

                        //    int cnt_count = contours.Length;
                        //    int point = 0;
                        //    // opcode controls whether we care about X or Y direction
                        //    // do some pointer trickery so we can operate on the
                        //    // points in a direction-agnostic manner
                        //    TouchState touchMask;

                        //    if (opcode == OpCode.IUP0)
                        //    {
                        //        //y -axis
                        //        touchMask = TouchState.Y;

                        //        //
                        //        for (int i = 0; i < cnt_count; ++i)
                        //        {
                        //            int endPoint = contours[i];
                        //            int firstPoint = point;
                        //            int firstTouched = -1;
                        //            int lastTouched = -1;

                        //            for (; point <= endPoint; point++)
                        //            {
                        //                // check whether this point has been touched
                        //                if ((points.TouchState[point] & touchMask) != 0)
                        //                {
                        //                    // if this is the first touched point in the contour, note it and continue
                        //                    if (firstTouched < 0)
                        //                    {
                        //                        firstTouched = point;
                        //                        lastTouched = point;
                        //                        continue;
                        //                    }

                        //                    // otherwise, interpolate all untouched points
                        //                    // between this point and our last touched point
                        //                    InterpolatePointsYAxis(currentPnts, originalPnts, lastTouched + 1, point - 1, lastTouched, point);
                        //                    lastTouched = point;
                        //                }
                        //            }

                        //            // check if we had any touched points at all in this contour
                        //            if (firstTouched >= 0)
                        //            {
                        //                // there are two cases left to handle:
                        //                // 1. there was only one touched point in the whole contour, in
                        //                //    which case we want to shift everything relative to that one
                        //                // 2. several touched points, in which case handle the gap from the
                        //                //    beginning to the first touched point and the gap from the last
                        //                //    touched point to the end of the contour
                        //                if (lastTouched == firstTouched)
                        //                {
                        //                    float delta = currentPnts[lastTouched].Y - originalPnts[lastTouched].Y;
                        //                    if (delta != 0.0f)
                        //                    {
                        //                        for (int n = firstPoint; n < lastTouched; n++)
                        //                        {
                        //                            currentPnts[n].OffsetY(delta);
                        //                        }
                        //                        for (int n = lastTouched + 1; n <= endPoint; n++)
                        //                        {
                        //                            currentPnts[n].OffsetY(delta);
                        //                        }

                        //                    }
                        //                }
                        //                else
                        //                {
                        //                    InterpolatePointsYAxis(currentPnts, originalPnts, lastTouched + 1, endPoint, lastTouched, firstTouched);
                        //                    if (firstTouched > 0)
                        //                    {
                        //                        InterpolatePointsYAxis(currentPnts, originalPnts, firstPoint, firstTouched - 1, lastTouched, firstTouched);
                        //                    }
                        //                }
                        //            }

                        //        }
                        //    }
                        //    else
                        //    {
                        //        //x-axis
                        //        touchMask = TouchState.X;
                        //        //
                        //        for (int i = 0; i < cnt_count; ++i)
                        //        {
                        //            int endPoint = contours[i];
                        //            int firstPoint = point;
                        //            int firstTouched = -1;
                        //            int lastTouched = -1;

                        //            for (; point <= endPoint; point++)
                        //            {
                        //                // check whether this point has been touched
                        //                if ((points.TouchState[point] & touchMask) != 0)
                        //                {
                        //                    // if this is the first touched point in the contour, note it and continue
                        //                    if (firstTouched < 0)
                        //                    {
                        //                        firstTouched = point;
                        //                        lastTouched = point;
                        //                        continue;
                        //                    }

                        //                    // otherwise, interpolate all untouched points
                        //                    // between this point and our last touched point
                        //                    InterpolatePointsXAxis(currentPnts, originalPnts, lastTouched + 1, point - 1, lastTouched, point);
                        //                    lastTouched = point;
                        //                }
                        //            }

                        //            // check if we had any touched points at all in this contour
                        //            if (firstTouched >= 0)
                        //            {
                        //                // there are two cases left to handle:
                        //                // 1. there was only one touched point in the whole contour, in
                        //                //    which case we want to shift everything relative to that one
                        //                // 2. several touched points, in which case handle the gap from the
                        //                //    beginning to the first touched point and the gap from the last
                        //                //    touched point to the end of the contour
                        //                if (lastTouched == firstTouched)
                        //                {
                        //                    float delta = currentPnts[lastTouched].X - originalPnts[lastTouched].X;
                        //                    if (delta != 0.0f)
                        //                    {
                        //                        for (int n = firstPoint; n < lastTouched; ++n)
                        //                        {
                        //                            currentPnts[n].OffsetX(delta);
                        //                        }
                        //                        for (int n = lastTouched + 1; n <= endPoint; ++n)
                        //                        {
                        //                            currentPnts[n].OffsetX(delta);
                        //                        }
                        //                    }
                        //                }
                        //                else
                        //                {
                        //                    InterpolatePointsXAxis(currentPnts, originalPnts, lastTouched + 1, endPoint, lastTouched, firstTouched);
                        //                    if (firstTouched > 0)
                        //                    {
                        //                        InterpolatePointsXAxis(currentPnts, originalPnts, firstPoint, firstTouched - 1, lastTouched, firstTouched);
                        //                    }
                        //                }
                        //            }
                        //        }
                        //    }
                        //}
                        //-----------------------------------------
                        unsafe
                        {

                            //unsafe version 
                            //TODO: provide manage version 
                            fixed (GlyphPointF* currentPtr = _points.Current)
                            fixed (GlyphPointF* originalPtr = _points.Original)
                            {
                                // opcode controls whether we care about X or Y direction
                                // do some pointer trickery so we can operate on the
                                // points in a direction-agnostic manner
                                TouchState touchMask;
                                byte* current;
                                byte* original;
                                if (opcode == OpCode.IUP0)
                                {
                                    touchMask = TouchState.Y;
                                    current = (byte*)&currentPtr->P.Y;
                                    original = (byte*)&originalPtr->P.Y;
                                }
                                else
                                {
                                    touchMask = TouchState.X;
                                    current = (byte*)&currentPtr->P.X;
                                    original = (byte*)&originalPtr->P.X;
                                }

                                var point = 0;
                                for (int i = 0; i < _contours.Length; i++)
                                {
                                    var endPoint = _contours[i];
                                    var firstPoint = point;
                                    var firstTouched = -1;
                                    var lastTouched = -1;

                                    for (; point <= endPoint; point++)
                                    {
                                        // check whether this point has been touched
                                        if ((_points.TouchState[point] & touchMask) != 0)
                                        {
                                            // if this is the first touched point in the contour, note it and continue
                                            if (firstTouched < 0)
                                            {
                                                firstTouched = point;
                                                lastTouched = point;
                                                continue;
                                            }

                                            // otherwise, interpolate all untouched points
                                            // between this point and our last touched point
                                            InterpolatePoints(current, original, lastTouched + 1, point - 1, lastTouched, point);
                                            lastTouched = point;
                                        }
                                    }

                                    // check if we had any touched points at all in this contour
                                    if (firstTouched >= 0)
                                    {
                                        // there are two cases left to handle:
                                        // 1. there was only one touched point in the whole contour, in
                                        //    which case we want to shift everything relative to that one
                                        // 2. several touched points, in which case handle the gap from the
                                        //    beginning to the first touched point and the gap from the last
                                        //    touched point to the end of the contour
                                        if (lastTouched == firstTouched)
                                        {
                                            var delta = *GetPoint(current, lastTouched) - *GetPoint(original, lastTouched);
                                            if (delta != 0.0f)
                                            {
                                                for (int j = firstPoint; j < lastTouched; j++)
                                                    *GetPoint(current, j) += delta;
                                                for (int j = lastTouched + 1; j <= endPoint; j++)
                                                    *GetPoint(current, j) += delta;
                                            }
                                        }
                                        else
                                        {
                                            InterpolatePoints(current, original, lastTouched + 1, endPoint, lastTouched, firstTouched);
                                            if (firstTouched > 0)
                                                InterpolatePoints(current, original, firstPoint, firstTouched - 1, lastTouched, firstTouched);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case OpCode.ISECT:
                        {
                            // move point P to the intersection of lines A and B
                            var b1 = _zp0.GetCurrent(_stack.Pop());
                            var b0 = _zp0.GetCurrent(_stack.Pop());
                            var a1 = _zp1.GetCurrent(_stack.Pop());
                            var a0 = _zp1.GetCurrent(_stack.Pop());
                            var index = _stack.Pop();

                            // calculate intersection using determinants: https://en.wikipedia.org/wiki/Line%E2%80%93line_intersection#Given_two_points_on_each_line
                            var da = a0 - a1;
                            var db = b0 - b1;
                            var den = (da.X * db.Y) - (da.Y * db.X);
                            if (Math.Abs(den) <= Epsilon)
                            {
                                // parallel lines; spec says to put the ppoint "into the middle of the two lines"
                                _zp2.Current[index].P = (a0 + a1 + b0 + b1) / 4;
                            }
                            else
                            {
                                var t = (a0.X * a1.Y) - (a0.Y * a1.X);
                                var u = (b0.X * b1.Y) - (b0.Y * b1.X);
                                var p = new Vector2(
                                    (t * db.X) - (da.X * u),
                                    (t * db.Y) - (da.Y * u)
                                );
                                _zp2.Current[index].P = p / den;
                            }
                            _zp2.TouchState[index] = TouchState.Both;
                        }
                        break;

                    // ==== STACK MANAGEMENT ====
                    case OpCode.DUP: _stack.Duplicate(); break;
                    case OpCode.POP: _stack.Pop(); break;
                    case OpCode.CLEAR: _stack.Clear(); break;
                    case OpCode.SWAP: _stack.Swap(); break;
                    case OpCode.DEPTH: _stack.Depth(); break;
                    case OpCode.CINDEX: _stack.Copy(); break;
                    case OpCode.MINDEX: _stack.Move(); break;
                    case OpCode.ROLL: _stack.Roll(); break;

                    // ==== FLOW CONTROL ====
                    case OpCode.IF:
                        {
                            // value is false; jump to the next else block or endif marker
                            // otherwise, we don't have to do anything; we'll keep executing this block
                            if (!_stack.PopBool())
                            {
                                int indent = 1;
                                while (indent > 0)
                                {
                                    opcode = SkipNext(ref stream);
                                    switch (opcode)
                                    {
                                        case OpCode.IF: indent++; break;
                                        case OpCode.EIF: indent--; break;
                                        case OpCode.ELSE:
                                            if (indent == 1)
                                                indent = 0;
                                            break;
                                    }
                                }
                            }
                        }
                        break;
                    case OpCode.ELSE:
                        {
                            // assume we hit the true statement of some previous if block
                            // if we had hit false, we would have jumped over this
                            int indent = 1;
                            while (indent > 0)
                            {
                                opcode = SkipNext(ref stream);
                                switch (opcode)
                                {
                                    case OpCode.IF: indent++; break;
                                    case OpCode.EIF: indent--; break;
                                }
                            }
                        }
                        break;
                    case OpCode.EIF: /* nothing to do */ break;
                    case OpCode.JROT:
                    case OpCode.JROF:
                        {
                            if (_stack.PopBool() == (opcode == OpCode.JROT))
                                stream.Jump(_stack.Pop() - 1);
                            else
                                _stack.Pop();    // ignore the offset
                        }
                        break;
                    case OpCode.JMPR: stream.Jump(_stack.Pop() - 1); break;

                    // ==== LOGICAL OPS ====
                    case OpCode.LT:
                        {
                            var b = _stack.Pop();
                            var a = _stack.Pop();
                            _stack.Push(a < b);
                        }
                        break;
                    case OpCode.LTEQ:
                        {
                            var b = _stack.Pop();
                            var a = _stack.Pop();
                            _stack.Push(a <= b);
                        }
                        break;
                    case OpCode.GT:
                        {
                            var b = _stack.Pop();
                            var a = _stack.Pop();
                            _stack.Push(a > b);
                        }
                        break;
                    case OpCode.GTEQ:
                        {
                            var b = _stack.Pop();
                            var a = _stack.Pop();
                            _stack.Push(a >= b);
                        }
                        break;
                    case OpCode.EQ:
                        {
                            var b = _stack.Pop();
                            var a = _stack.Pop();
                            _stack.Push(a == b);
                        }
                        break;
                    case OpCode.NEQ:
                        {
                            var b = _stack.Pop();
                            var a = _stack.Pop();
                            _stack.Push(a != b);
                        }
                        break;
                    case OpCode.AND:
                        {
                            var b = _stack.PopBool();
                            var a = _stack.PopBool();
                            _stack.Push(a && b);
                        }
                        break;
                    case OpCode.OR:
                        {
                            var b = _stack.PopBool();
                            var a = _stack.PopBool();
                            _stack.Push(a || b);
                        }
                        break;
                    case OpCode.NOT: _stack.Push(!_stack.PopBool()); break;
                    case OpCode.ODD:
                        {
                            var value = (int)Round(_stack.PopFloat());
                            _stack.Push(value % 2 != 0);
                        }
                        break;
                    case OpCode.EVEN:
                        {
                            var value = (int)Round(_stack.PopFloat());
                            _stack.Push(value % 2 == 0);
                        }
                        break;

                    // ==== ARITHMETIC ====
                    case OpCode.ADD:
                        {
                            var b = _stack.Pop();
                            var a = _stack.Pop();
                            _stack.Push(a + b);
                        }
                        break;
                    case OpCode.SUB:
                        {
                            var b = _stack.Pop();
                            var a = _stack.Pop();
                            _stack.Push(a - b);
                        }
                        break;
                    case OpCode.DIV:
                        {
                            var b = _stack.Pop();
                            if (b == 0)
                                throw new InvalidTrueTypeFontException("Division by zero.");

                            var a = _stack.Pop();
                            var result = ((long)a << 6) / b;
                            _stack.Push((int)result);
                        }
                        break;
                    case OpCode.MUL:
                        {
                            var b = _stack.Pop();
                            var a = _stack.Pop();
                            var result = ((long)a * b) >> 6;
                            _stack.Push((int)result);
                        }
                        break;
                    case OpCode.ABS: _stack.Push(Math.Abs(_stack.Pop())); break;
                    case OpCode.NEG: _stack.Push(-_stack.Pop()); break;
                    case OpCode.FLOOR: _stack.Push(_stack.Pop() & ~63); break;
                    case OpCode.CEILING: _stack.Push((_stack.Pop() + 63) & ~63); break;
                    case OpCode.MAX: _stack.Push(Math.Max(_stack.Pop(), _stack.Pop())); break;
                    case OpCode.MIN: _stack.Push(Math.Min(_stack.Pop(), _stack.Pop())); break;

                    // ==== FUNCTIONS ====
                    case OpCode.FDEF:
                        {
                            if (!allowFunctionDefs || inFunction)
                                throw new InvalidTrueTypeFontException("Can't define functions here.");

                            _functions[_stack.Pop()] = stream;
                            while (SkipNext(ref stream) != OpCode.ENDF) ;
                        }
                        break;
                    case OpCode.IDEF:
                        {
                            if (!allowFunctionDefs || inFunction)
                                throw new InvalidTrueTypeFontException("Can't define functions here.");

                            _instructionDefs[_stack.Pop()] = stream;
                            while (SkipNext(ref stream) != OpCode.ENDF) ;
                        }
                        break;
                    case OpCode.ENDF:
                        {
                            if (!inFunction)
                                throw new InvalidTrueTypeFontException("Found invalid ENDF marker outside of a function definition.");
                            return;
                        }
                    case OpCode.CALL:
                    case OpCode.LOOPCALL:
                        {
                            _callStackSize++;
                            if (_callStackSize > MaxCallStack)
                                throw new InvalidTrueTypeFontException("Stack overflow; infinite recursion?");

                            var function = _functions[_stack.Pop()];
                            var count = opcode == OpCode.LOOPCALL ? _stack.Pop() : 1;
                            for (int i = 0; i < count; i++)
                                Execute(function, true, false);
                            _callStackSize--;
                        }
                        break;

                    // ==== ROUNDING ====
                    // we don't have "engine compensation" so the variants are unnecessary
                    case OpCode.ROUND0:
                    case OpCode.ROUND1:
                    case OpCode.ROUND2:
                    case OpCode.ROUND3: _stack.Push(Round(_stack.PopFloat())); break;
                    case OpCode.NROUND0:
                    case OpCode.NROUND1:
                    case OpCode.NROUND2:
                    case OpCode.NROUND3: break;

                    // ==== DELTA EXCEPTIONS ====
                    case OpCode.DELTAC1:
                    case OpCode.DELTAC2:
                    case OpCode.DELTAC3:
                        {
                            var last = _stack.Pop();
                            for (int i = 1; i <= last; i++)
                            {
                                var cvtIndex = _stack.Pop();
                                var arg = _stack.Pop();

                                // upper 4 bits of the 8-bit arg is the relative ppem
                                // the opcode specifies the base to add to the ppem
                                var triggerPpem = (arg >> 4) & 0xF;
                                triggerPpem += (opcode - OpCode.DELTAC1) * 16;
                                triggerPpem += _state.DeltaBase;

                                // if the current ppem matches the trigger, apply the exception
                                if (_ppem == triggerPpem)
                                {
                                    // the lower 4 bits of the arg is the amount to shift
                                    // it's encoded such that 0 isn't an allowable value (who wants to shift by 0 anyway?)
                                    var amount = (arg & 0xF) - 8;
                                    if (amount >= 0)
                                        amount++;
                                    amount *= 1 << (6 - _state.DeltaShift);

                                    // update the CVT
                                    CheckIndex(cvtIndex, _controlValueTable.Length);
                                    _controlValueTable[cvtIndex] += F26Dot6ToFloat(amount);
                                }
                            }
                        }
                        break;
                    case OpCode.DELTAP1:
                    case OpCode.DELTAP2:
                    case OpCode.DELTAP3:
                        {
                            var last = _stack.Pop();
                            for (int i = 1; i <= last; i++)
                            {
                                var pointIndex = _stack.Pop();
                                var arg = _stack.Pop();

                                // upper 4 bits of the 8-bit arg is the relative ppem
                                // the opcode specifies the base to add to the ppem
                                var triggerPpem = (arg >> 4) & 0xF;
                                triggerPpem += _state.DeltaBase;
                                if (opcode != OpCode.DELTAP1)
                                    triggerPpem += (opcode - OpCode.DELTAP2 + 1) * 16;

                                // if the current ppem matches the trigger, apply the exception
                                if (_ppem == triggerPpem)
                                {
                                    // the lower 4 bits of the arg is the amount to shift
                                    // it's encoded such that 0 isn't an allowable value (who wants to shift by 0 anyway?)
                                    var amount = (arg & 0xF) - 8;
                                    if (amount >= 0)
                                        amount++;
                                    amount *= 1 << (6 - _state.DeltaShift);

                                    MovePoint(_zp0, pointIndex, F26Dot6ToFloat(amount));
                                }
                            }
                        }
                        break;

                    // ==== MISCELLANEOUS ====
                    case OpCode.DEBUG: _stack.Pop(); break;
                    case OpCode.GETINFO:
                        {
                            var selector = _stack.Pop();
                            var result = 0;
                            if ((selector & 0x1) != 0)
                            {
                                // pretend we are MS Rasterizer v35
                                result = 35;
                            }

                            // TODO: rotation and stretching
                            //if ((selector & 0x2) != 0)
                            //if ((selector & 0x4) != 0)

                            // we're always rendering in grayscale
                            if ((selector & 0x20) != 0)
                                result |= 1 << 12;

                            // TODO: ClearType flags

                            _stack.Push(result);
                        }
                        break;

                    default:
                        if (opcode >= OpCode.MIRP)
                            MoveIndirectRelative(opcode - OpCode.MIRP);
                        else if (opcode >= OpCode.MDRP)
                            MoveDirectRelative(opcode - OpCode.MDRP);
                        else
                        {
                            // check if this is a runtime-defined opcode
                            var index = (int)opcode;
                            if (index > _instructionDefs.Length || !_instructionDefs[index].IsValid)
                                throw new InvalidTrueTypeFontException("Unknown opcode in font program.");

                            _callStackSize++;
                            if (_callStackSize > MaxCallStack)
                                throw new InvalidTrueTypeFontException("Stack overflow; infinite recursion?");

                            Execute(_instructionDefs[index], true, false);
                            _callStackSize--;
                        }
                        break;
                }
            }
        }

        int CheckIndex(int index, int length)
        {
            if (index < 0 || index >= length)
                throw new InvalidTrueTypeFontException();
            return index;
        }

        float ReadCvt() { return _controlValueTable[CheckIndex(_stack.Pop(), _controlValueTable.Length)]; }

        void OnVectorsUpdated()
        {
            _fdotp = (float)Vector2.Dot(_state.Freedom, _state.Projection);
            if (Math.Abs(_fdotp) < Epsilon)
                _fdotp = 1.0f;
        }

        void SetFreedomVectorToAxis(int axis)
        {
            _state.Freedom = axis == 0 ? Vector2.UnitY : Vector2.UnitX;
            OnVectorsUpdated();
        }

        void SetProjectionVectorToAxis(int axis)
        {
            _state.Projection = axis == 0 ? Vector2.UnitY : Vector2.UnitX;
            _state.DualProjection = _state.Projection;

            OnVectorsUpdated();
        }

        void SetVectorToLine(int mode, bool dual)
        {
            // mode here should be as follows:
            // 0: SPVTL0
            // 1: SPVTL1
            // 2: SFVTL0
            // 3: SFVTL1
            var index1 = _stack.Pop();
            var index2 = _stack.Pop();
            var p1 = _zp2.GetCurrent(index1);
            var p2 = _zp1.GetCurrent(index2);

            var line = p2 - p1;
            if (line.LengthSquared() == 0)
            {
                // invalid; just set to whatever
                if (mode >= 2)
                    _state.Freedom = Vector2.UnitX;
                else
                {
                    _state.Projection = Vector2.UnitX;
                    _state.DualProjection = Vector2.UnitX;
                }
            }
            else
            {
                // if mode is 1 or 3, we want a perpendicular vector
                if ((mode & 0x1) != 0)
                    line = new Vector2(-line.Y, line.X);
                line = Vector2.Normalize(line);

                if (mode >= 2)
                    _state.Freedom = line;
                else
                {
                    _state.Projection = line;
                    _state.DualProjection = line;
                }
            }

            // set the dual projection vector using original points
            if (dual)
            {
                p1 = _zp2.GetOriginal(index1);
                p2 = _zp2.GetOriginal(index2);
                line = p2 - p1;

                if (line.LengthSquared() == 0)
                    _state.DualProjection = Vector2.UnitX;
                else
                {
                    if ((mode & 0x1) != 0)
                        line = new Vector2(-line.Y, line.X);

                    _state.DualProjection = Vector2.Normalize(line);
                }
            }

            OnVectorsUpdated();
        }

        Zone GetZoneFromStack()
        {
            switch (_stack.Pop())
            {
                case 0: return _twilight;
                case 1: return _points;
                default: throw new InvalidTrueTypeFontException("Invalid zone pointer.");
            }
        }

        void SetSuperRound(float period)
        {
            // mode is a bunch of packed flags
            // bits 7-6 are the period multiplier
            var mode = _stack.Pop();
            switch (mode & 0xC0)
            {
                case 0: roundPeriod = period / 2; break;
                case 0x40: roundPeriod = period; break;
                case 0x80: roundPeriod = period * 2; break;
                default: throw new InvalidTrueTypeFontException("Unknown rounding period multiplier.");
            }

            // bits 5-4 are the phase
            switch (mode & 0x30)
            {
                case 0: _roundPhase = 0; break;
                case 0x10: _roundPhase = roundPeriod / 4; break;
                case 0x20: _roundPhase = roundPeriod / 2; break;
                case 0x30: _roundPhase = roundPeriod * 3 / 4; break;
            }

            // bits 3-0 are the threshold
            if ((mode & 0xF) == 0)
                _roundThreshold = roundPeriod - 1;
            else
                _roundThreshold = ((mode & 0xF) - 4) * roundPeriod / 8;
        }

        void MoveIndirectRelative(int flags)
        {
            // this instruction tries to make the current distance between a given point
            // and the reference point rp0 be equivalent to the same distance in the original outline
            // there are a bunch of flags that control how that distance is measured
            var cvt = ReadCvt();
            var pointIndex = _stack.Pop();

            if (Math.Abs(cvt - _state.SingleWidthValue) < _state.SingleWidthCutIn)
            {
                if (cvt >= 0)
                    cvt = _state.SingleWidthValue;
                else
                    cvt = -_state.SingleWidthValue;
            }

            // if we're looking at the twilight zone we need to prepare the points there
            var originalReference = _zp0.GetOriginal(_state.Rp0);
            if (_zp1.IsTwilight)
            {
                var initialValue = originalReference + _state.Freedom * cvt;
                _zp1.Original[pointIndex].P = initialValue;
                _zp1.Current[pointIndex].P = initialValue;
            }

            var point = _zp1.GetCurrent(pointIndex);
            var originalDistance = DualProject(_zp1.GetOriginal(pointIndex) - originalReference);
            var currentDistance = Project(point - _zp0.GetCurrent(_state.Rp0));

            if (_state.AutoFlip && Math.Sign(originalDistance) != Math.Sign(cvt))
                cvt = -cvt;

            // if bit 2 is set, round the distance and look at the cut-in value
            var distance = cvt;
            if ((flags & 0x4) != 0)
            {
                // only perform cut-in tests when both points are in the same zone
                if (_zp0.IsTwilight == _zp1.IsTwilight && Math.Abs(cvt - originalDistance) > _state.ControlValueCutIn)
                    cvt = originalDistance;
                distance = Round(cvt);
            }

            // if bit 3 is set, constrain to the minimum distance
            if ((flags & 0x8) != 0)
            {
                if (originalDistance >= 0)
                    distance = Math.Max(distance, _state.MinDistance);
                else
                    distance = Math.Min(distance, -_state.MinDistance);
            }

            // move the point
            MovePoint(_zp1, pointIndex, distance - currentDistance);
            _state.Rp1 = _state.Rp0;
            _state.Rp2 = pointIndex;
            if ((flags & 0x10) != 0)
                _state.Rp0 = pointIndex;
        }

        void MoveDirectRelative(int flags)
        {
            // determine the original distance between the two reference points
            var pointIndex = _stack.Pop();
            var p1 = _zp0.GetOriginal(_state.Rp0);
            var p2 = _zp1.GetOriginal(pointIndex);
            var originalDistance = DualProject(p2 - p1);

            // single width cutin test
            if (Math.Abs(originalDistance - _state.SingleWidthValue) < _state.SingleWidthCutIn)
            {
                if (originalDistance >= 0)
                    originalDistance = _state.SingleWidthValue;
                else
                    originalDistance = -_state.SingleWidthValue;
            }

            // if bit 2 is set, perform rounding
            var distance = originalDistance;
            if ((flags & 0x4) != 0)
                distance = Round(distance);

            // if bit 3 is set, constrain to the minimum distance
            if ((flags & 0x8) != 0)
            {
                if (originalDistance >= 0)
                    distance = Math.Max(distance, _state.MinDistance);
                else
                    distance = Math.Min(distance, -_state.MinDistance);
            }

            // move the point
            originalDistance = Project(_zp1.GetCurrent(pointIndex) - _zp0.GetCurrent(_state.Rp0));
            MovePoint(_zp1, pointIndex, distance - originalDistance);
            _state.Rp1 = _state.Rp0;
            _state.Rp2 = pointIndex;
            if ((flags & 0x10) != 0)
                _state.Rp0 = pointIndex;
        }

        Vector2 ComputeDisplacement(int mode, out Zone zone, out int point)
        {
            // compute displacement of the reference point
            if ((mode & 1) == 0)
            {
                zone = _zp1;
                point = _state.Rp2;
            }
            else
            {
                zone = _zp0;
                point = _state.Rp1;
            }

            var distance = Project(zone.GetCurrent(point) - zone.GetOriginal(point));
            return distance * _state.Freedom / _fdotp;
        }

        TouchState GetTouchState()
        {
            var touch = TouchState.None;
            if (_state.Freedom.X != 0)
                touch = TouchState.X;
            if (_state.Freedom.Y != 0)
                touch |= TouchState.Y;

            return touch;
        }

        void ShiftPoints(Vector2 displacement)
        {
            var touch = GetTouchState();
            for (int i = 0; i < _state.Loop; i++)
            {
                var pointIndex = _stack.Pop();
                _zp2.Current[pointIndex].P += displacement;
                _zp2.TouchState[pointIndex] |= touch;
            }
            _state.Loop = 1;
        }

        void MovePoint(Zone zone, int index, float distance)
        {
            var point = zone.GetCurrent(index) + distance * _state.Freedom / _fdotp;
            var touch = GetTouchState();
            zone.Current[index].P = point;
            zone.TouchState[index] |= touch;
        }

        float Round(float value)
        {
            switch (_state.RoundState)
            {
                case RoundMode.ToGrid: return value >= 0 ? (float)Math.Round(value) : -(float)Math.Round(-value);
                case RoundMode.ToHalfGrid: return value >= 0 ? (float)Math.Floor(value) + 0.5f : -((float)Math.Floor(-value) + 0.5f);
                case RoundMode.ToDoubleGrid: return value >= 0 ? (float)(Math.Round(value * 2, MidpointRounding.AwayFromZero) / 2) : -(float)(Math.Round(-value * 2, MidpointRounding.AwayFromZero) / 2);
                case RoundMode.DownToGrid: return value >= 0 ? (float)Math.Floor(value) : -(float)Math.Floor(-value);
                case RoundMode.UpToGrid: return value >= 0 ? (float)Math.Ceiling(value) : -(float)Math.Ceiling(-value);
                case RoundMode.Super:
                case RoundMode.Super45:
                    float result;
                    if (value >= 0)
                    {
                        result = value - _roundPhase + _roundThreshold;
                        result = (float)Math.Truncate(result / roundPeriod) * roundPeriod;
                        result += _roundPhase;
                        if (result < 0)
                            result = _roundPhase;
                    }
                    else
                    {
                        result = -value - _roundPhase + _roundThreshold;
                        result = -(float)Math.Truncate(result / roundPeriod) * roundPeriod;
                        result -= _roundPhase;
                        if (result > 0)
                            result = -_roundPhase;
                    }
                    return result;

                default: return value;
            }
        }

        float Project(Vector2 point) { return (float)Vector2.Dot(point, _state.Projection); }
        float DualProject(Vector2 point) { return (float)Vector2.Dot(point, _state.DualProjection); }

        static OpCode SkipNext(ref InstructionStream stream)
        {
            // grab the next opcode, and if it's one of the push instructions skip over its arguments
            var opcode = stream.NextOpCode();
            switch (opcode)
            {
                case OpCode.NPUSHB:
                case OpCode.PUSHB1:
                case OpCode.PUSHB2:
                case OpCode.PUSHB3:
                case OpCode.PUSHB4:
                case OpCode.PUSHB5:
                case OpCode.PUSHB6:
                case OpCode.PUSHB7:
                case OpCode.PUSHB8:
                    {
                        var count = opcode == OpCode.NPUSHB ? stream.NextByte() : opcode - OpCode.PUSHB1 + 1;
                        for (int i = 0; i < count; i++)
                            stream.NextByte();
                    }
                    break;
                case OpCode.NPUSHW:
                case OpCode.PUSHW1:
                case OpCode.PUSHW2:
                case OpCode.PUSHW3:
                case OpCode.PUSHW4:
                case OpCode.PUSHW5:
                case OpCode.PUSHW6:
                case OpCode.PUSHW7:
                case OpCode.PUSHW8:
                    {
                        var count = opcode == OpCode.NPUSHW ? stream.NextByte() : opcode - OpCode.PUSHW1 + 1;
                        for (int i = 0; i < count; i++)
                            stream.NextWord();
                    }
                    break;
            }

            return opcode;
        }

        static unsafe void InterpolatePoints(byte* current, byte* original, int start, int end, int ref1, int ref2)
        {
            if (start > end)
                return;

            // figure out how much the two reference points
            // have been shifted from their original positions
            float delta1, delta2;
            float lower = *GetPoint(original, ref1);
            float upper = *GetPoint(original, ref2);
            if (lower > upper)
            {
                var temp = lower;
                lower = upper;
                upper = temp;

                delta1 = *GetPoint(current, ref2) - lower;
                delta2 = *GetPoint(current, ref1) - upper;
            }
            else
            {
                delta1 = *GetPoint(current, ref1) - lower;
                delta2 = *GetPoint(current, ref2) - upper;
            }

            float lowerCurrent = delta1 + lower;
            float upperCurrent = delta2 + upper;
            float scale = (upperCurrent - lowerCurrent) / (upper - lower);

            for (int i = start; i <= end; i++)
            {
                // three cases: if it's to the left of the lower reference point or to
                // the right of the upper reference point, do a shift based on that ref point.
                // otherwise, interpolate between the two of them
                float pos = *GetPoint(original, i);
                if (pos <= lower)
                {
                    pos += delta1;
                }
                else if (pos >= upper)
                {
                    pos += delta2;
                }
                else
                {
                    pos = lowerCurrent + (pos - lower) * scale;
                }
                *GetPoint(current, i) = pos;
            }
        }

        static void InterpolatePointsXAxis(GlyphPointF[] current, GlyphPointF[] original, int start, int end, int ref1, int ref2)
        {
            if (start > end)
                return;

            // figure out how much the two reference points
            // have been shifted from their original positions
            float delta1, delta2;
            float lower = original[ref1].X;
            float upper = original[ref2].X;
            if (lower > upper)
            {
                var temp = lower;
                lower = upper;
                upper = temp;

                delta1 = current[ref2].X - lower;
                delta2 = current[ref1].X - upper;
            }
            else
            {
                delta1 = current[ref1].X - lower;
                delta2 = current[ref2].X - upper;
            }

            float lowerCurrent = delta1 + lower;
            float upperCurrent = delta2 + upper;
            float scale = (upperCurrent - lowerCurrent) / (upper - lower);

            for (int i = start; i <= end; i++)
            {
                // three cases: if it's to the left of the lower reference point or to
                // the right of the upper reference point, do a shift based on that ref point.
                // otherwise, interpolate between the two of them
                float pos = original[i].X;
                if (pos <= lower)
                {
                    pos += delta1;
                }
                else if (pos >= upper)
                {
                    pos += delta2;
                }
                else
                {
                    pos = lowerCurrent + (pos - lower) * scale;
                }
                current[i].UpdateX(pos);
            }
        }
        static void InterpolatePointsYAxis(GlyphPointF[] current, GlyphPointF[] original, int start, int end, int ref1, int ref2)
        {
            if (start > end)
                return;

            // figure out how much the two reference points
            // have been shifted from their original positions
            float delta1, delta2;
            float lower = original[ref1].Y;
            float upper = original[ref2].Y;
            if (lower > upper)
            {
                float temp = lower; //swap
                lower = upper;
                upper = temp;

                delta1 = current[ref2].Y - lower;
                delta2 = current[ref1].Y - upper;
            }
            else
            {
                delta1 = current[ref1].Y - lower;
                delta2 = current[ref2].Y - upper;
            }

            float lowerCurrent = delta1 + lower;
            float upperCurrent = delta2 + upper;
            float scale = (upperCurrent - lowerCurrent) / (upper - lower);

            for (int i = start; i <= end; i++)
            {
                // three cases: if it's to the left of the lower reference point or to
                // the right of the upper reference point, do a shift based on that ref point.
                // otherwise, interpolate between the two of them
                float pos = original[i].Y;
                if (pos <= lower)
                {
                    pos += delta1;
                }
                else if (pos >= upper)
                {
                    pos += delta2;
                }
                else
                {
                    pos = lowerCurrent + (pos - lower) * scale;
                }
                current[i].UpdateY(pos);
            }
        }
        static float F2Dot14ToFloat(int value) => (short)value / 16384.0f;
        static int FloatToF2Dot14(float value) => (int)(uint)(short)Math.Round(value * 16384.0f);
        static float F26Dot6ToFloat(int value) => value / 64.0f;
        static int FloatToF26Dot6(float value) => (int)Math.Round(value * 64.0f);

        //TODO: review here again
        unsafe static float* GetPoint(byte* data, int index) => (float*)(data + sizeof(GlyphPointF) * index);

        static readonly float Sqrt2Over2 = (float)(Math.Sqrt(2) / 2);

        const int MaxCallStack = 128;
        const float Epsilon = 0.000001f;

        struct InstructionStream
        {
            readonly byte[] _instructions;
            int _ip;

            public bool IsValid => _instructions != null;
            public bool Done => _ip >= _instructions.Length;

            public InstructionStream(byte[] instructions)
            {
                _instructions = instructions;
                _ip = 0;
            }

            public int NextByte()
            {
                if (Done)
                    throw new InvalidTrueTypeFontException();
                return _instructions[_ip++];
            }

            public OpCode NextOpCode() => (OpCode)NextByte();
            public int NextWord() => (short)(ushort)(NextByte() << 8 | NextByte());
            public void Jump(int offset) { _ip += offset; }
        }

        struct GraphicsState
        {
            public Vector2 Freedom;
            public Vector2 DualProjection;
            public Vector2 Projection;
            public InstructionControlFlags InstructionControl;
            public RoundMode RoundState;
            public float MinDistance;
            public float ControlValueCutIn;
            public float SingleWidthCutIn;
            public float SingleWidthValue;
            public int DeltaBase;
            public int DeltaShift;
            public int Loop;
            public int Rp0;
            public int Rp1;
            public int Rp2;
            public bool AutoFlip;

            public void Reset()
            {
                Freedom = Vector2.UnitX;
                Projection = Vector2.UnitX;
                DualProjection = Vector2.UnitX;
                InstructionControl = InstructionControlFlags.None;
                RoundState = RoundMode.ToGrid;
                MinDistance = 1.0f;
                ControlValueCutIn = 17.0f / 16.0f;
                SingleWidthCutIn = 0.0f;
                SingleWidthValue = 0.0f;
                DeltaBase = 9;
                DeltaShift = 3;
                Loop = 1;
                Rp0 = Rp1 = Rp2 = 0;
                AutoFlip = true;
            }
        }

        class ExecutionStack
        {
            int[] _s;
            int _count;

            public ExecutionStack(int maxStack)
            {
                _s = new int[maxStack];
            }

            public int Peek() => Peek(0);
            public bool PopBool() => Pop() != 0;
            public float PopFloat() => F26Dot6ToFloat(Pop());
            public void Push(bool value) => Push(value ? 1 : 0);
            public void Push(float value) => Push(FloatToF26Dot6(value));

            public void Clear() => _count = 0;
            public void Depth() => Push(_count);
            public void Duplicate() => Push(Peek());
            public void Copy() => Copy(Pop() - 1);
            public void Copy(int index) => Push(Peek(index));
            public void Move() => Move(Pop() - 1);
            public void Roll() => Move(2);

            public void Move(int index)
            {
                var val = Peek(index);
                for (int i = _count - index - 1; i < _count - 1; i++)
                    _s[i] = _s[i + 1];
                _s[_count - 1] = val;
            }

            public void Swap()
            {
                if (_count < 2)
                    throw new InvalidTrueTypeFontException();

                var tmp = _s[_count - 1];
                _s[_count - 1] = _s[_count - 2];
                _s[_count - 2] = tmp;
            }

            public void Push(int value)
            {
                if (_count == _s.Length)
                    throw new InvalidTrueTypeFontException();
                _s[_count++] = value;
            }

            public int Pop()
            {
                if (_count == 0)
                    throw new InvalidTrueTypeFontException();
                return _s[--_count];
            }

            public int Peek(int index)
            {
                if (index < 0 || index >= _count)
                    throw new InvalidTrueTypeFontException();
                return _s[_count - index - 1];
            }
        }

        readonly struct Zone
        {
            public readonly GlyphPointF[] Current;
            public readonly GlyphPointF[] Original;
            public readonly TouchState[] TouchState;
            public readonly bool IsTwilight;

            public Zone(GlyphPointF[] points, bool isTwilight)
            {
                IsTwilight = isTwilight;
                Current = points;
                Original = (GlyphPointF[])points.Clone();
                TouchState = new TouchState[points.Length];
            }

            public Vector2 GetCurrent(int index) => Current[index].P;
            public Vector2 GetOriginal(int index) => Original[index].P;
        }

        enum RoundMode
        {
            ToHalfGrid,
            ToGrid,
            ToDoubleGrid,
            DownToGrid,
            UpToGrid,
            Off,
            Super,
            Super45
        }

        [Flags]
        enum InstructionControlFlags
        {
            None,
            InhibitGridFitting = 0x1,
            UseDefaultGraphicsState = 0x2
        }

        [Flags]
        enum TouchState
        {
            None = 0,
            X = 0x1,
            Y = 0x2,
            Both = X | Y
        }

        enum OpCode : byte
        {
            SVTCA0,
            SVTCA1,
            SPVTCA0,
            SPVTCA1,
            SFVTCA0,
            SFVTCA1,
            SPVTL0,
            SPVTL1,
            SFVTL0,
            SFVTL1,
            SPVFS,
            SFVFS,
            GPV,
            GFV,
            SFVTPV,
            ISECT,
            SRP0,
            SRP1,
            SRP2,
            SZP0,
            SZP1,
            SZP2,
            SZPS,
            SLOOP,
            RTG,
            RTHG,
            SMD,
            ELSE,
            JMPR,
            SCVTCI,
            SSWCI,
            SSW,
            DUP,
            POP,
            CLEAR,
            SWAP,
            DEPTH,
            CINDEX,
            MINDEX,
            ALIGNPTS,
            /* unused: 0x28 */
            UTP = 0x29,
            LOOPCALL,
            CALL,
            FDEF,
            ENDF,
            MDAP0,
            MDAP1,
            IUP0,
            IUP1,
            SHP0,
            SHP1,
            SHC0,
            SHC1,
            SHZ0,
            SHZ1,
            SHPIX,
            IP,
            MSIRP0,
            MSIRP1,
            ALIGNRP,
            RTDG,
            MIAP0,
            MIAP1,
            NPUSHB,
            NPUSHW,
            WS,
            RS,
            WCVTP,
            RCVT,
            GC0,
            GC1,
            SCFS,
            MD0,
            MD1,
            MPPEM,
            MPS,
            FLIPON,
            FLIPOFF,
            DEBUG,
            LT,
            LTEQ,
            GT,
            GTEQ,
            EQ,
            NEQ,
            ODD,
            EVEN,
            IF,
            EIF,
            AND,
            OR,
            NOT,
            DELTAP1,
            SDB,
            SDS,
            ADD,
            SUB,
            DIV,
            MUL,
            ABS,
            NEG,
            FLOOR,
            CEILING,
            ROUND0,
            ROUND1,
            ROUND2,
            ROUND3,
            NROUND0,
            NROUND1,
            NROUND2,
            NROUND3,
            WCVTF,
            DELTAP2,
            DELTAP3,
            DELTAC1,
            DELTAC2,
            DELTAC3,
            SROUND,
            S45ROUND,
            JROT,
            JROF,
            ROFF,
            /* unused: 0x7B */
            RUTG = 0x7C,
            RDTG,
            SANGW,
            AA,
            FLIPPT,
            FLIPRGON,
            FLIPRGOFF,
            /* unused: 0x83 - 0x84 */
            SCANCTRL = 0x85,
            SDPVTL0,
            SDPVTL1,
            GETINFO,
            IDEF,
            ROLL,
            MAX,
            MIN,
            SCANTYPE,
            INSTCTRL,
            /* unused: 0x8F - 0xAF */
            PUSHB1 = 0xB0,
            PUSHB2,
            PUSHB3,
            PUSHB4,
            PUSHB5,
            PUSHB6,
            PUSHB7,
            PUSHB8,
            PUSHW1,
            PUSHW2,
            PUSHW3,
            PUSHW4,
            PUSHW5,
            PUSHW6,
            PUSHW7,
            PUSHW8,
            MDRP,           // range of 32 values, 0xC0 - 0xDF,
            MIRP = 0xE0     // range of 32 values, 0xE0 - 0xFF
        }
    }
}
