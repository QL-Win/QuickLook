//Apache2, 2018, apache/pdfbox Authors ( https://github.com/apache/pdfbox) 
//MIT, 2018-present, WinterDev  

using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace Typography.OpenFont.CFF
{

    public class CffEvaluationEngine
    {

        float _scale = 1;//default 
        readonly Stack<Type2EvaluationStack> _evalStackPool = new Stack<Type2EvaluationStack>();

        class PxScaleGlyphTx : IGlyphTranslator
        {
            readonly float _scale;
            readonly IGlyphTranslator _tx;

            bool _is_contour_opened;

            public PxScaleGlyphTx(float scale, IGlyphTranslator tx)
            {
                _scale = scale;
                _tx = tx;
            }

            public void BeginRead(int contourCount)
            {
                _tx.BeginRead(contourCount);
            }

            public void CloseContour()
            {
                _is_contour_opened = false;
                _tx.CloseContour();
            }

            public void Curve3(float x1, float y1, float x2, float y2)
            {
                _is_contour_opened = true;
                _tx.Curve3(x1 * _scale, y1 * _scale, x2 * _scale, y2 * _scale);
            }

            public void Curve4(float x1, float y1, float x2, float y2, float x3, float y3)
            {
                _is_contour_opened = true;
                _tx.Curve4(x1 * _scale, y1 * _scale, x2 * _scale, y2 * _scale, x3 * _scale, y3 * _scale);
            }

            public void EndRead()
            {
                _tx.EndRead();
            }

            public void LineTo(float x1, float y1)
            {
                _is_contour_opened = true;
                _tx.LineTo(x1 * _scale, y1 * _scale);
            }

            public void MoveTo(float x0, float y0)
            {
                _tx.MoveTo(x0 * _scale, y0 * _scale);
            }
            //

            public bool IsContourOpened => _is_contour_opened;
        }

        public CffEvaluationEngine()
        {

        }
        public void Run(IGlyphTranslator tx, Cff1GlyphData glyphData, float scale = 1)
        {
            Run(tx, glyphData.GlyphInstructions, scale);
        }
        internal void Run(IGlyphTranslator tx, Type2Instruction[] instructionList, float scale = 1)
        {

            //all fields are set to new values*** 

            _scale = scale;

            double currentX = 0, currentY = 0;


            var scaleTx = new PxScaleGlyphTx(scale, tx);
            //
            scaleTx.BeginRead(0);//unknown contour count  
            //
            Run(scaleTx, instructionList, ref currentX, ref currentY);
            //

            //
            //some cff end without closing the latest contour?

            if (scaleTx.IsContourOpened)
            {
                scaleTx.CloseContour();
            }

            scaleTx.EndRead();

        }
        void Run(IGlyphTranslator tx, Type2Instruction[] instructionList, ref double currentX, ref double currentY)
        {
            //recursive ***

            Type2EvaluationStack evalStack = GetFreeEvalStack(); //**
#if DEBUG
            //evalStack.dbugGlyphIndex = instructionList.dbugGlyphIndex;
#endif
            evalStack._currentX = currentX;
            evalStack._currentY = currentY;
            evalStack.GlyphTranslator = tx;

            for (int i = 0; i < instructionList.Length; ++i)
            {
                Type2Instruction inst = instructionList[i];
                //----------
                //this part is our extension to the original
                int merge_flags = inst.Op >> 6;//upper 2 bits is our extension flags
                switch (merge_flags)
                {
                    case 0: //nothing
                        break;
                    case 1:
                        evalStack.Push(inst.Value);
                        break;
                    case 2:
                        evalStack.Push((short)(inst.Value >> 16));
                        evalStack.Push((short)(inst.Value >> 0));
                        break;
                    case 3:
                        evalStack.Push((sbyte)(inst.Value >> 24));
                        evalStack.Push((sbyte)(inst.Value >> 16));
                        evalStack.Push((sbyte)(inst.Value >> 8));
                        evalStack.Push((sbyte)(inst.Value >> 0));
                        break;
                }
                //----------
                switch ((OperatorName)((inst.Op & 0b111111)))//we use only 6 lower bits for op_name
                {
                    default: throw new OpenFontNotSupportedException();
                    case OperatorName.GlyphWidth:
                        //TODO: 
                        break;
                    case OperatorName.LoadInt:
                        evalStack.Push(inst.Value);
                        break;
                    case OperatorName.LoadSbyte4:
                        //4 consecutive sbyte
                        evalStack.Push((sbyte)(inst.Value >> 24));
                        evalStack.Push((sbyte)(inst.Value >> 16));
                        evalStack.Push((sbyte)(inst.Value >> 8));
                        evalStack.Push((sbyte)(inst.Value >> 0));
                        break;
                    case OperatorName.LoadSbyte3:
                        evalStack.Push((sbyte)(inst.Value >> 24));
                        evalStack.Push((sbyte)(inst.Value >> 16));
                        evalStack.Push((sbyte)(inst.Value >> 8));
                        break;
                    case OperatorName.LoadShort2:
                        evalStack.Push((short)(inst.Value >> 16));
                        evalStack.Push((short)(inst.Value >> 0));
                        break;
                    case OperatorName.LoadFloat:
                        evalStack.Push(inst.ReadValueAsFixed1616());
                        break;
                    case OperatorName.endchar:
                        evalStack.EndChar();
                        break;
                    case OperatorName.flex: evalStack.Flex(); break;
                    case OperatorName.hflex: evalStack.H_Flex(); break;
                    case OperatorName.hflex1: evalStack.H_Flex1(); break;
                    case OperatorName.flex1: evalStack.Flex1(); break;
                    //-------------------------
                    //4.4: Arithmetic Operators
                    case OperatorName.abs: evalStack.Op_Abs(); break;
                    case OperatorName.add: evalStack.Op_Add(); break;
                    case OperatorName.sub: evalStack.Op_Sub(); break;
                    case OperatorName.div: evalStack.Op_Div(); break;
                    case OperatorName.neg: evalStack.Op_Neg(); break;
                    case OperatorName.random: evalStack.Op_Random(); break;
                    case OperatorName.mul: evalStack.Op_Mul(); break;
                    case OperatorName.sqrt: evalStack.Op_Sqrt(); break;
                    case OperatorName.drop: evalStack.Op_Drop(); break;
                    case OperatorName.exch: evalStack.Op_Exch(); break;
                    case OperatorName.index: evalStack.Op_Index(); break;
                    case OperatorName.roll: evalStack.Op_Roll(); break;
                    case OperatorName.dup: evalStack.Op_Dup(); break;

                    //-------------------------
                    //4.5: Storage Operators 
                    case OperatorName.put: evalStack.Put(); break;
                    case OperatorName.get: evalStack.Get(); break;
                    //-------------------------
                    //4.6: Conditional
                    case OperatorName.and: evalStack.Op_And(); break;
                    case OperatorName.or: evalStack.Op_Or(); break;
                    case OperatorName.not: evalStack.Op_Not(); break;
                    case OperatorName.eq: evalStack.Op_Eq(); break;
                    case OperatorName.ifelse: evalStack.Op_IfElse(); break;
                    // 
                    case OperatorName.rlineto: evalStack.R_LineTo(); break;
                    case OperatorName.hlineto: evalStack.H_LineTo(); break;
                    case OperatorName.vlineto: evalStack.V_LineTo(); break;
                    case OperatorName.rrcurveto: evalStack.RR_CurveTo(); break;
                    case OperatorName.hhcurveto: evalStack.HH_CurveTo(); break;
                    case OperatorName.hvcurveto: evalStack.HV_CurveTo(); break;
                    case OperatorName.rcurveline: evalStack.R_CurveLine(); break;
                    case OperatorName.rlinecurve: evalStack.R_LineCurve(); break;
                    case OperatorName.vhcurveto: evalStack.VH_CurveTo(); break;
                    case OperatorName.vvcurveto: evalStack.VV_CurveTo(); break;
                    //-------------------------------------------------------------------                     
                    case OperatorName.rmoveto: evalStack.R_MoveTo(); break;
                    case OperatorName.hmoveto: evalStack.H_MoveTo(); break;
                    case OperatorName.vmoveto: evalStack.V_MoveTo(); break;
                    //-------------------------------------------------------------------
                    //4.3 Hint Operators
                    case OperatorName.hstem: evalStack.H_Stem(); break;
                    case OperatorName.vstem: evalStack.V_Stem(); break;
                    case OperatorName.vstemhm: evalStack.V_StemHM(); break;
                    case OperatorName.hstemhm: evalStack.H_StemHM(); break;
                    //--------------------------
                    case OperatorName.hintmask1: evalStack.HintMask1(inst.Value); break;
                    case OperatorName.hintmask2: evalStack.HintMask2(inst.Value); break;
                    case OperatorName.hintmask3: evalStack.HintMask3(inst.Value); break;
                    case OperatorName.hintmask4: evalStack.HintMask4(inst.Value); break;
                    case OperatorName.hintmask_bits: evalStack.HintMaskBits(inst.Value); break;
                    //------------------------------
                    case OperatorName.cntrmask1: evalStack.CounterSpaceMask1(inst.Value); break;
                    case OperatorName.cntrmask2: evalStack.CounterSpaceMask2(inst.Value); break;
                    case OperatorName.cntrmask3: evalStack.CounterSpaceMask3(inst.Value); break;
                    case OperatorName.cntrmask4: evalStack.CounterSpaceMask4(inst.Value); break;
                    case OperatorName.cntrmask_bits: evalStack.CounterSpaceMaskBits(inst.Value); break;

                    //-------------------------
                    //4.7: Subroutine Operators
                    case OperatorName._return:
                        {
                            //***
                            //don't forget to return _evalStack's currentX, currentY to prev evl context
                            currentX = evalStack._currentX;
                            currentY = evalStack._currentY;
                            evalStack.Ret();
                        }
                        break;
                    //should not occur!-> since we replace this in parsing step
                    case OperatorName.callgsubr:
                    case OperatorName.callsubr:
                        throw new OpenFontNotSupportedException();
                }
            }


            ReleaseEvalStack(evalStack);//****
        }

        Type2EvaluationStack GetFreeEvalStack()
        {
            if (_evalStackPool.Count > 0)
            {
                return _evalStackPool.Pop();
            }
            else
            {
                return new Type2EvaluationStack();
            }
        }
        void ReleaseEvalStack(Type2EvaluationStack evalStack)
        {
            evalStack.Reset();
            _evalStackPool.Push(evalStack);
        }
    }

    class Type2EvaluationStack
    {

        internal double _currentX;
        internal double _currentY;

        double[] _argStack = new double[50];
        int _currentIndex = 0; //current stack index

        IGlyphTranslator _glyphTranslator;
#if DEBUG

        public int dbugGlyphIndex;
#endif
        public Type2EvaluationStack()
        {
        }

        public void Reset()
        {
            _currentIndex = 0;
            _currentX = _currentY = 0;
            _glyphTranslator = null;
        }
        public IGlyphTranslator GlyphTranslator
        {
            get => _glyphTranslator;
            set => _glyphTranslator = value;
        }
        public void Push(double value)
        {
            _argStack[_currentIndex] = value;
            _currentIndex++;
        }
        public void Push(int value)
        {
            _argStack[_currentIndex] = value;
            _currentIndex++;
        }
        //Many operators take their arguments from the bottom-most
        //entries in the Type 2 argument stack; this behavior is indicated
        //by the stack bottom symbol ‘| -’ appearing to the left of the first
        //argument.Operators that clear the argument stack are
        //indicated by the stack bottom symbol ‘| -’ in the result position
        //of the operator definition

        //[NOTE4]:
        //The first stack - clearing operator, which must be one of...

        //hstem, hstemhm, vstem, vstemhm, cntrmask, 
        //hintmask, hmoveto, vmoveto, rmoveto, or endchar,

        //...
        //takes an additional argument — the width(as
        //described earlier), which may be expressed as zero or one numeric
        //argument

        //-------------------------
        //4.1: Path Construction Operators

        /// <summary>
        /// rmoveto
        /// </summary>
        public void R_MoveTo()
        {
            //|- dx1 dy1 rmoveto(21) |-

            //moves the current point to
            //a position at the relative coordinates(dx1, dy1) 
            //see [NOTE4]
#if DEBUG
            if (_currentIndex != 2)
            {
                throw new OpenFontNotSupportedException();
            }
#endif


            _glyphTranslator.CloseContour();
            _glyphTranslator.MoveTo((float)(_currentX += _argStack[0]), (float)(_currentY += _argStack[1]));

            _currentIndex = 0; //clear stack 
        }

        /// <summary>
        /// hmoveto
        /// </summary>
        public void H_MoveTo()
        {
            //|- dx1 hmoveto(22) |-

            //moves the current point 
            //dx1 units in the horizontal direction
            //see [NOTE4] 

            _glyphTranslator.MoveTo((float)(_currentX += _argStack[0]), (float)_currentY);
            _currentIndex = 0; //clear stack 
        }
        public void V_MoveTo()
        {
            //|- dy1 vmoveto (4) |-
            //moves the current point 
            //dy1 units in the vertical direction.
            //see [NOTE4] 
#if DEBUG
            if (_currentIndex > 1)
            {
                throw new OpenFontNotSupportedException();
            }
#endif
            _glyphTranslator.MoveTo((float)_currentX, (float)(_currentY += _argStack[0]));
            _currentIndex = 0; //clear stack 
        }
        public void R_LineTo()
        {
            //|- {dxa dya}+  rlineto (5) |-

            //appends a line from the current point to 
            //a position at the relative coordinates dxa, dya. 

            //Additional rlineto operations are 
            //performed for all subsequent argument pairs. 

            //The number of 
            //lines is determined from the number of arguments on the stack
#if DEBUG
            if ((_currentIndex % 2) != 0)
            {
                throw new OpenFontNotSupportedException();
            }
#endif
            for (int i = 0; i < _currentIndex;)
            {
                _glyphTranslator.LineTo((float)(_currentX += _argStack[i]), (float)(_currentY += _argStack[i + 1]));
                i += 2;
            }
            _currentIndex = 0; //clear stack 
        }
        public void H_LineTo()
        {

            //|- dx1 {dya dxb}*  hlineto (6) |-
            //|- {dxa dyb}+  hlineto (6) |-

            //appends a horizontal line of length 
            //dx1 to the current point. 

            //With an odd number of arguments, subsequent argument pairs 
            //are interpreted as alternating values of 
            //dy and dx, for which additional lineto
            //operators draw alternating vertical and 
            //horizontal lines.

            //With an even number of arguments, the 
            //arguments are interpreted as alternating horizontal and 
            //vertical lines. The number of lines is determined from the 
            //number of arguments on the stack.

            //first elem
            int i = 0;
            if ((_currentIndex % 2) != 0)
            {
                //|- dx1 {dya dxb}*  hlineto (6) |-
                //odd number                
                _glyphTranslator.LineTo((float)(_currentX += _argStack[i]), (float)_currentY);
                i++;
                for (; i < _currentIndex;)
                {
                    _glyphTranslator.LineTo((float)(_currentX), (float)(_currentY += _argStack[i]));
                    _glyphTranslator.LineTo((float)(_currentX += _argStack[i + 1]), (float)(_currentY));
                    i += 2;
                }
            }
            else
            {
                //even number
                //|- {dxa dyb}+  hlineto (6) |-
                for (; i < _currentIndex;)
                {
                    //line to
                    _glyphTranslator.LineTo((float)(_currentX += _argStack[i]), (float)(_currentY));
                    _glyphTranslator.LineTo((float)(_currentX), (float)(_currentY += _argStack[i + 1]));
                    //
                    i += 2;
                }
            }
            _currentIndex = 0; //clear stack 
        }
        public void V_LineTo()
        {
            //|- dy1 {dxa dyb}*  vlineto (7) |-
            //|- {dya dxb}+  vlineto (7) |-

            //appends a vertical line of length 
            //dy1 to the current point. 

            //With an odd number of arguments, subsequent argument pairs are 
            //interpreted as alternating values of dx and dy, for which additional 
            //lineto operators draw alternating horizontal and 
            //vertical lines.

            //With an even number of arguments, the 
            //arguments are interpreted as alternating vertical and 
            //horizontal lines. The number of lines is determined from the 
            //number of arguments on the stack. 
            //first elem
            int i = 0;
            if ((_currentIndex % 2) != 0)
            {
                //|- dy1 {dxa dyb}*  vlineto (7) |-
                //odd number                
                _glyphTranslator.LineTo((float)_currentX, (float)(_currentY += _argStack[i]));
                i++;

                for (; i < _currentIndex;)
                {
                    //line to
                    _glyphTranslator.LineTo((float)(_currentX += _argStack[i]), (float)(_currentY));
                    _glyphTranslator.LineTo((float)(_currentX), (float)(_currentY += _argStack[i + 1]));
                    //
                    i += 2;
                }
            }
            else
            {
                //even number
                //|- {dya dxb}+  vlineto (7) |-
                for (; i < _currentIndex;)
                {
                    //line to
                    _glyphTranslator.LineTo((float)(_currentX), (float)(_currentY += _argStack[i]));
                    _glyphTranslator.LineTo((float)(_currentX += _argStack[i + 1]), (float)(_currentY));
                    //
                    i += 2;
                }
            }
            _currentIndex = 0; //clear stack 
        }

        public void RR_CurveTo()
        {

            //|- {dxa dya dxb dyb dxc dyc}+  rrcurveto (8) |-

            //appends a Bézier curve, defined by dxa...dyc, to the current point.

            //For each subsequent set of six arguments, an additional 
            //curve is appended to the current point. 

            //The number of curve segments is determined from 
            //the number of arguments on the number stack and 
            //is limited only by the size of the number stack


            //All Bézier curve path segments are drawn using six arguments,
            //dxa, dya, dxb, dyb, dxc, dyc; where dxa and dya are relative to
            //the current point, and all subsequent arguments are relative to
            //the previous point.A number of the curve operators take
            //advantage of the situation where some tangent points are
            //horizontal or vertical(and hence the value is zero), thus
            //reducing the number of arguments needed.

            int i = 0;
#if DEBUG
            if ((_currentIndex % 6) != 0)
            {
                throw new OpenFontNotSupportedException();
            }

#endif
            double curX = _currentX;
            double curY = _currentY;
            for (; i < _currentIndex;)
            {
                _glyphTranslator.Curve4(
                    (float)(curX += _argStack[i + 0]), (float)(curY += _argStack[i + 1]), //dxa,dya
                    (float)(curX += _argStack[i + 2]), (float)(curY += _argStack[i + 3]), //dxb,dyb
                    (float)(curX += _argStack[i + 4]), (float)(curY += _argStack[i + 5])  //dxc,dyc
                    );
                //
                i += 6;
            }
            _currentX = curX;
            _currentY = curY;
            _currentIndex = 0; //clear stack 
        }
        public void HH_CurveTo()
        {

            //|- dy1? {dxa dxb dyb dxc}+ hhcurveto (27) |-

            //appends one or more Bézier curves, as described by the 
            //dxa...dxc set of arguments, to the current point. 
            //For each curve, if there are 4 arguments, 
            //the curve starts and ends horizontal. 


            //The first curve need not start horizontal (the odd argument 
            //case). Note the argument order for the odd argument case

            int i = 0;
            int count = _currentIndex;
            double curX = _currentX;
            double curY = _currentY;

            if ((count % 2) != 0)
            {
                //odd number      
                _glyphTranslator.Curve4(
                   (float)(curX += _argStack[1]), (float)(curY += _argStack[0]), //dxa,+dy1?
                   (float)(curX += _argStack[2]), (float)(curY += _argStack[3]), //dxb,dyb
                   (float)(curX += _argStack[4]), (float)(curY)  //dxc,+0
                   );
                i += 5;
                count -= 5;
            }

            //next
            while (count > 0)
            {
                _glyphTranslator.Curve4(
                    (float)(curX += _argStack[i + 0]), (float)(curY), //dxa,+0
                    (float)(curX += _argStack[i + 1]), (float)(curY += _argStack[i + 2]), //dxb,dyb
                    (float)(curX += _argStack[i + 3]), (float)(curY)  //dxc,+0
                    );
                //
                i += 4;
                count -= 4;
            }
            _currentX = curX;
            _currentY = curY;
            _currentIndex = 0; //clear stack  
        }
        public void HV_CurveTo()
        {
            //|- dx1 dx2 dy2 dy3 {dya dxb dyb dxc dxd dxe dye dyf}* dxf? hvcurveto (31) |-

            //|- {dxa dxb dyb dyc dyd dxe dye dxf}+ dyf? hvcurveto (31) |-

            //appends one or more Bézier curves to the current point.

            //The tangent for the first Bézier must be horizontal, and the second 
            //must be vertical (except as noted below). 

            //If there is a multiple of four arguments, the curve starts
            //horizontal and ends vertical.Note that the curves alternate
            //between start horizontal, end vertical, and start vertical, and
            //end horizontal.The last curve(the odd argument case) need not
            //end horizontal/ vertical.

#if DEBUG

#endif
            int i = 0;
            int remainder = 0;

            switch (remainder = (_currentIndex % 8))
            {
                default: throw new OpenFontNotSupportedException();
                case 0:
                case 1:
                    {
                        //|- {dxa dxb dyb dyc dyd dxe dye dxf}+ dyf? hvcurveto (31) |-

                        double curX = _currentX;
                        double curY = _currentY;

                        int count = _currentIndex;
                        while (count > 0)
                        {
                            _glyphTranslator.Curve4(
                                (float)(curX += _argStack[i + 0]), (float)(curY), //+dxa,0
                                (float)(curX += _argStack[i + 1]), (float)(curY += _argStack[i + 2]), //dxb,dyb
                                (float)(curX), (float)(curY += _argStack[i + 3])  //+0,dyc
                                );


                            if (count == 9)
                            {
                                //last cycle
                                _glyphTranslator.Curve4(
                                 (float)(curX), (float)(curY += _argStack[i + 4]), //+0,dyd
                                 (float)(curX += _argStack[i + 5]), (float)(curY += _argStack[i + 6]), //dxe,dye
                                 (float)(curX += _argStack[i + 7]), (float)(curY += _argStack[i + 8])  //dxf,+dyf
                                 );
                                //
                                count -= 9;
                                i += 9;
                            }
                            else
                            {
                                _glyphTranslator.Curve4(
                                (float)(curX), (float)(curY += _argStack[i + 4]), //+0,dyd
                                (float)(curX += _argStack[i + 5]), (float)(curY += _argStack[i + 6]), //dxe,dye
                                (float)(curX += _argStack[i + 7]), (float)(curY)  //dxf,+0
                                );
                                //
                                count -= 8;
                                i += 8;
                            }

                        }
                        _currentX = curX;
                        _currentY = curY;
                    }
                    break;

                case 4:
                case 5:
                    {

                        //|- dx1 dx2 dy2 dy3 {dya dxb dyb dxc dxd dxe dye dyf}* dxf? hvcurveto (31) |-

                        //If there is a multiple of four arguments, the curve starts
                        //horizontal and ends vertical.
                        //Note that the curves alternate between start horizontal, end vertical, and start vertical, and
                        //end horizontal.The last curve(the odd argument case) need not
                        //end horizontal/ vertical.

                        double curX = _currentX;
                        double curY = _currentY;

                        int count = _currentIndex;

                        if (count == 5)
                        {
                            //last one
                            _glyphTranslator.Curve4(
                               (float)(curX += _argStack[i + 0]), (float)(curY), //dx1
                               (float)(curX += _argStack[i + 1]), (float)(curY += _argStack[i + 2]), //dx2,dy2
                               (float)(curX += _argStack[i + 4]), (float)(curY += _argStack[i + 3])  //dx3,dy3
                               );

                            count -= 5;
                            i += 5;
                        }
                        else
                        {
                            _glyphTranslator.Curve4(
                               (float)(curX += _argStack[i + 0]), (float)(curY), //dx1
                               (float)(curX += _argStack[i + 1]), (float)(curY += _argStack[i + 2]), //dx2,dy2
                               (float)(curX), (float)(curY += _argStack[i + 3])  //dy3
                               );

                            count -= 4;
                            i += 4;
                        }


                        while (count > 0)
                        {
                            _glyphTranslator.Curve4(
                                (float)(curX), (float)(curY += _argStack[i + 0]), //0,dya
                                (float)(curX += _argStack[i + 1]), (float)(curY += _argStack[i + 2]), //dxb,dyb
                                (float)(curX += _argStack[i + 3]), (float)(curY)  //dxc, +0
                                );

                            if (count == 9)
                            {
                                //last cycle
                                _glyphTranslator.Curve4(
                                 (float)(curX += _argStack[i + 4]), (float)(curY), //dxd,0
                                 (float)(curX += _argStack[i + 5]), (float)(curY += _argStack[i + 6]), //dxe,dye
                                 (float)(curX += _argStack[i + 8]), (float)(curY += _argStack[i + 7])  //dxf,dyf
                                 );
                                i += 9;
                                count -= 9;
                            }
                            else
                            {
                                _glyphTranslator.Curve4(
                                (float)(curX += _argStack[i + 4]), (float)(curY), //dxd,0
                                (float)(curX += _argStack[i + 5]), (float)(curY += _argStack[i + 6]), //dxe,dye
                                (float)(curX), (float)(curY += _argStack[i + 7])  //0,dyf
                                );
                                //
                                i += 8;
                                count -= 8;
                            }

                        }

                        _currentX = curX;
                        _currentY = curY;
                    }
                    break;
            }


            _currentIndex = 0; //clear stack 
        }
        public void R_CurveLine()
        {

#if DEBUG

#endif
            //|- { dxa dya dxb dyb dxc dyc} +dxd dyd rcurveline(24) |-
            //is equivalent to one rrcurveto for each set of six arguments
            //dxa...dyc, followed by exactly one rlineto using
            //the dxd, dyd arguments.

            //The number of curves is determined from the count
            //on the argument stack.

            double curX = _currentX;
            double curY = _currentY;

            int i = 0;
            int count = _currentIndex;
            while (count > 0)
            {

                _glyphTranslator.Curve4(
                    (float)(curX += _argStack[i + 0]), (float)(curY += _argStack[i + 1]), //dxa,dya
                    (float)(curX += _argStack[i + 2]), (float)(curY += _argStack[i + 3]), //dxb,dyb
                    (float)(curX += _argStack[i + 4]), (float)(curY += _argStack[i + 5])  //dxc,dyc
                    );
                //
                i += 6;
                count -= 6;

                if (count == 2)
                {
                    //last one
                    _glyphTranslator.LineTo((float)(curX += _argStack[i]), (float)(curY += _argStack[i + 1]));
                    break;//exit while
                }
            }
            _currentX = curX;
            _currentY = curY;
            _currentIndex = 0; //clear stack 

        }
        public void R_LineCurve()
        {
#if DEBUG

#endif
            //|- { dxa dya} +dxb dyb dxc dyc dxd dyd rlinecurve(25) |-

            //is equivalent to one rlineto for each pair of arguments beyond
            //the six arguments dxb...dyd needed for the one
            //rrcurveto command.The number of lines is determined from the count of
            //items on the argument stack.

            double curX = _currentX;
            double curY = _currentY;

            int i = 0;
            int count = _currentIndex;
            while (count > 0)
            {
                _glyphTranslator.LineTo(
                    (float)(curX += _argStack[i + 0]), (float)(curY += _argStack[i + 1]));
                //
                i += 2;
                count -= 2;

                if (count == 6)
                {
                    //last one
                    _glyphTranslator.Curve4(
                    (float)(curX += _argStack[i + 0]), (float)(curY += _argStack[i + 1]), //dxa,dya
                    (float)(curX += _argStack[i + 2]), (float)(curY += _argStack[i + 3]), //dxb,dyb
                    (float)(curX += _argStack[i + 4]), (float)(curY += _argStack[i + 5])  //dxc,dyc
                    );
                    break;//exit while
                }
            }
            _currentX = curX;
            _currentY = curY;
            _currentIndex = 0; //clear stack 

        }
        public void VH_CurveTo()
        {

#if DEBUG


#endif

            //|- dy1 dx2 dy2 dx3 {dxa dxb dyb dyc dyd dxe dye dxf}* dyf? vhcurveto (30) |-


            //|- {dya dxb dyb dxc dxd dxe dye dyf}+ dxf? vhcurveto (30) |- 

            //appends one or more Bézier curves to the current point, where 
            //the first tangent is vertical and the second tangent is horizontal.

            //This command is the complement of 
            //hvcurveto; 

            //see the description of hvcurveto for more information.
            int i = 0;
            int remainder = 0;

            switch (remainder = (_currentIndex % 8))
            {
                default: throw new OpenFontNotSupportedException();
                case 0:
                case 1:
                    {
                        //|- {dya dxb dyb dxc dxd dxe dye dyf}+ dxf? vhcurveto (30) |-  
                        double curX = _currentX;
                        double curY = _currentY;
                        int ncount = _currentIndex;
                        while (ncount > 0)
                        {
                            _glyphTranslator.Curve4(
                                (float)(curX), (float)(curY += _argStack[i + 0]), //+0,dya
                                (float)(curX += _argStack[i + 1]), (float)(curY += _argStack[i + 2]), //dxb,dyb
                                (float)(curX += _argStack[i + 3]), (float)(curY)  //dxc,+0
                                );

                            if (ncount == 9)
                            {
                                //last cycle
                                _glyphTranslator.Curve4(
                                 (float)(curX += _argStack[i + 4]), (float)(curY), //dxd,+0
                                 (float)(curX += _argStack[i + 5]), (float)(curY += _argStack[i + 6]), //dxe,dye
                                 (float)(curX += _argStack[i + 8]), (float)(curY += _argStack[i + 7])  //+dxf,dyf
                                 );
                                //
                                i += 9;
                                ncount -= 9;
                            }
                            else
                            {
                                _glyphTranslator.Curve4(
                                 (float)(curX += _argStack[i + 4]), (float)(curY), //dxd,+0
                                 (float)(curX += _argStack[i + 5]), (float)(curY += _argStack[i + 6]), //dxe,dye
                                 (float)(curX), (float)(curY += _argStack[i + 7])  //+0,dyf
                                 );
                                //
                                i += 8;
                                ncount -= 8;
                            }
                        }
                        _currentX = curX;
                        _currentY = curY;
                    }
                    break;

                case 4:
                case 5:
                    {

                        //|- dy1 dx2 dy2 dx3 {dxa dxb dyb dyc dyd dxe dye dxf}* dyf? vhcurveto (30) |-
                        double curX = _currentX;
                        double curY = _currentY;

                        int ncount = _currentIndex;
                        if (ncount == 5)
                        {
                            //only 1
                            _glyphTranslator.Curve4(
                              (float)(curX), (float)(curY += _argStack[i + 0]), //dy1
                              (float)(curX += _argStack[i + 1]), (float)(curY += _argStack[i + 2]), //dx2,dy2
                              (float)(curX += _argStack[i + 3]), (float)(curY += _argStack[i + 4]) //dx3,dyf
                              );
                            i += 5;
                            ncount -= 5;
                        }
                        else
                        {
                            _glyphTranslator.Curve4(
                              (float)(curX), (float)(curY += _argStack[i + 0]), //dy1
                              (float)(curX += _argStack[i + 1]), (float)(curY += _argStack[i + 2]), //dx2,dy2
                              (float)(curX += _argStack[i + 3]), (float)(curY) //dx3
                              );
                            i += 4;
                            ncount -= 4;
                        }

                        while (ncount > 0)
                        {
                            //line to

                            _glyphTranslator.Curve4(
                                (float)(curX += _argStack[i + 0]), (float)(curY), //dxa,
                                (float)(curX += _argStack[i + 1]), (float)(curY += _argStack[i + 2]), //dxb,dyb
                                (float)(curX), (float)(curY += _argStack[i + 3])  //+0, dyc
                                );
                            if (ncount == 9)
                            {
                                //last cycle
                                _glyphTranslator.Curve4(
                                    (float)(curX), (float)(curY += _argStack[i + 4]), //+0,dyd
                                    (float)(curX += _argStack[i + 5]), (float)(curY += _argStack[i + 6]), //dxe,dye
                                    (float)(curX += _argStack[i + 7]), (float)(curY += _argStack[i + 8])  //dxf,dyf
                                    );
                                i += 9;
                                ncount -= 9;
                            }
                            else
                            {
                                _glyphTranslator.Curve4(
                                    (float)(curX), (float)(curY += _argStack[i + 4]), //+0,dyd
                                    (float)(curX += _argStack[i + 5]), (float)(curY += _argStack[i + 6]), //dxe,dye
                                    (float)(curX += _argStack[i + 7]), (float)(curY)  //dxf,0
                                    );
                                i += 8;
                                ncount -= 8;
                            }
                        }
                        _currentX = curX;
                        _currentY = curY;
                    }
                    break;
            }

            _currentIndex = 0; //clear stack 


        }
        public void VV_CurveTo()
        {
            // |- dx1? {dya dxb dyb dyc}+  vvcurveto (26) |-
            //appends one or more curves to the current point. 
            //If the argument count is a multiple of four, the curve starts and ends vertical. 
            //If the argument count is odd, the first curve does not begin with a vertical tangent.

            int i = 0;
            int count = _currentIndex;

            double curX = _currentX;
            double curY = _currentY;

            if ((count % 2) != 0)
            {
                //odd number      
                _glyphTranslator.Curve4(
                   (float)(curX += _argStack[0]), (float)(curY += _argStack[1]), //dx1?,+dya
                   (float)(curX += _argStack[2]), (float)(curY += _argStack[3]), //dxb,dyb
                   (float)(curX), (float)(curY += _argStack[4])  //+0,+dyc
                   );
                i += 5;
                count -= 5;
            }

            while (count > 0)
            {
                _glyphTranslator.Curve4(
                    (float)(curX), (float)(curY += _argStack[i + 0]), //+0,dya
                    (float)(curX += _argStack[i + 1]), (float)(curY += _argStack[i + 2]), //dxb,dyb
                    (float)(curX), (float)(curY += _argStack[i + 3])  //+0,dyc
                    );
                //
                i += 4;
                count -= 4;
            }
            _currentX = curX;
            _currentY = curY;
            _currentIndex = 0; //clear stack
        }
        public void EndChar()
        {
            _currentIndex = 0;
        }
        public void Flex()
        {
            //|- dx1 dy1 dx2 dy2 dx3 dy3 dx4 dy4 dx5 dy5 dx6 dy6 fd flex (12 35) |-
            //causes two Bézier curves, as described by the arguments(as
            //shown in Figure 2 below), to be rendered as a straight line when
            //the flex depth is less than fd / 100 device pixels, and as curved lines
            // when the flex depth is greater than or equal to fd/ 100 device pixels


            _currentIndex = 0; //clear stack 
        }
        public void H_Flex()
        {
            //|- dx1 dx2 dy2 dx3 dx4 dx5 dx6 hflex (12 34) |- 
            //causes the two curves described by the arguments
            //dx1...dx6  to be rendered as a straight line when
            //the flex depth is less than 0.5(that is, fd is 50) device pixels,
            //and as curved lines when the flex depth is greater than or equal to 0.5 device pixels. 

            //hflex is used when the following are all true:
            //a) the starting and ending points, first and last control points
            //have the same y value.
            //b) the joining point and the neighbor control points have
            //the same y value.
            //c) the flex depth is 50.

            _currentIndex = 0; //clear stack
        }
        public void H_Flex1()
        {
            //|- dx1 dy1 dx2 dy2 dx3 dx4 dx5 dy5 dx6 hflex1 (12 36) |-

            //causes the two curves described by the arguments to be 
            //rendered as a straight line when the flex depth is less than 0.5 
            //device pixels, and as curved lines when the flex depth is greater 
            //than or equal to 0.5 device pixels.

            //hflex1 is used if the conditions for hflex
            //are not met but all of the following are true:

            //a) the starting and ending points have the same y value,
            //b) the joining point and the neighbor control points have 
            //the same y value.
            //c) the flex depth is 50.
            _currentIndex = 0; //clear stack
        }
        public void Flex1()
        {
            //|- dx1 dy1 dx2 dy2 dx3 dy3 dx4 dy4 dx5 dy5 d6 flex1 (12 37) |

            //causes the two curves described by the arguments to be
            //rendered as a straight line when the flex depth is less than 0.5
            //device pixels, and as curved lines when the flex depth is greater
            //than or equal to 0.5 device pixels.

            //The d6 argument will be either a dx or dy value, depending on
            //the curve(see Figure 3). To determine the correct value, 
            //compute the distance from the starting point(x, y), the first
            //point of the first curve, to the last flex control point(dx5, dy5)
            //by summing all the arguments except d6; call this(dx, dy).If
            //abs(dx) > abs(dy), then the last point’s x-value is given by d6, and
            //its y - value is equal to y.
            //  Otherwise, the last point’s x-value is equal to x and its y-value is given by d6.


            _currentIndex = 0; //clear stack
        }


        //-------------------------------------------------------------------
        //4.3 Hint Operators
        public void H_Stem()
        {
            //|- y dy {dya dyb}*  hstem (1) |-


#if DEBUG
            if ((_currentIndex % 2) != 0)
            {
                throw new OpenFontNotSupportedException();
            }
#endif
            //hintCount += _currentIndex / 2;
            _currentIndex = 0; //clear stack
        }
        public void V_Stem()
        {
            //|- x dx {dxa dxb}*  vstem (3) |-
#if DEBUG
            if ((_currentIndex % 2) != 0)
            {
                throw new OpenFontNotSupportedException();
            }
#endif
            //hintCount += _currentIndex / 2;
            _currentIndex = 0; //clear stack
        }
        public void V_StemHM()
        {

            //|- x dx {dxa dxb}* vstemhm (23) |-
#if DEBUG
            if ((_currentIndex % 2) != 0)
            {
                throw new OpenFontNotSupportedException();
            }
#endif
            //hintCount += _currentIndex / 2;
            _currentIndex = 0; //clear stack
        }
        public void H_StemHM()
        {
            //|- y dy {dya dyb}*  hstemhm (18) |-
#if DEBUG
            if ((_currentIndex % 2) != 0)
            {
                throw new OpenFontNotSupportedException();
            }
#endif
            //hintCount += _currentIndex / 2;
            //has the same meaning as 
            //hstem (1),
            //except that it must be used 
            //in place of hstem  if the charstring contains one or more 
            //hintmask operators.
            _currentIndex = 0; //clear stack
        }

        //----------------------------------------
        //hintmask | -hintmask(19 + mask) | -
        //The mask data bytes are defined as follows:
        //• The number of data bytes is exactly the number needed, one
        //bit per hint, to reference the number of stem hints declared
        //at the beginning of the charstring program.
        //• Each bit of the mask, starting with the most-significant bit of
        //the first byte, represents the corresponding hint zone in the
        //order in which the hints were declared at the beginning of
        //the charstring.
        //• For each bit in the mask, a value of ‘1’ specifies that the
        //corresponding hint shall be active. A bit value of ‘0’ specifies
        //that the hint shall be inactive.
        //• Unused bits in the mask, if any, must be zero.

        public void HintMask1(int hintMaskValue)
        {
            //specifies which hints are active and which are not active. If any
            //hints overlap, hintmask must be used to establish a nonoverlapping
            //subset of hints.
            //hintmask may occur any number of
            //times in a charstring. Path operators occurring after a hintmask
            //are influenced by the new hint set, but the current point is not
            //moved. If stem hint zones overlap and are not properly
            //managed by use of the hintmask operator, the results are
            //undefined. 

            //|- hintmask (19 + mask) |- 
            _currentIndex = 0; //clear stack
        }
        public void HintMask2(int hintMaskValue)
        {
            _currentIndex = 0; //clear stack
        }
        public void HintMask3(int hintMaskValue)
        {
            _currentIndex = 0; //clear stack
        }
        public void HintMask4(int hintMaskValue)
        {
            _currentIndex = 0; //clear stack
        }
        public void HintMaskBits(int bitCount)
        {

            //calculate bytes need by 
            //bytes need = (bitCount+7)/8 
#if DEBUG
            if (_currentIndex != (bitCount + 7) / 8)
            {

            }
#endif
            _currentIndex = 0; //clear stack
        }
        //----------------------------------------
        //|- cntrmask(20 + mask) |-

        //specifies the counter spaces to be controlled, and their relative
        //priority.The mask bits in the bytes, following the operator, 
        //reference the stem hint declarations; the most significant bit of
        //the first byte refers to the first stem hint declared, through to
        //the last hint declaration.The counters to be controlled are
        //those that are delimited by the referenced stem hints.Bits set to
        //1 in the first cntrmask command have top priority; subsequent
        //cntrmask commands specify lower priority counters(see Figure
        //1 and the accompanying example). 
        public void CounterSpaceMask1(int cntMaskValue)
        {
            _currentIndex = 0;//clear stack
        }
        public void CounterSpaceMask2(int cntMaskValue)
        {
            _currentIndex = 0;//clear stack
        }
        public void CounterSpaceMask3(int cntMaskValue)
        {
            _currentIndex = 0;//clear stack
        }
        public void CounterSpaceMask4(int cntMaskValue)
        {
            _currentIndex = 0;//clear stack
        }
        public void CounterSpaceMaskBits(int bitCount)
        {
            //calculate bytes need by 
            //bytes need = (bitCount+7)/8 
#if DEBUG
            if (_currentIndex != (bitCount + 7) / 8)
            {

            }
#endif

            _currentIndex = 0;//clear stack
        }
        //----------------------------------------



        //4.4: Arithmetic Operators

        //case Type2Operator2.abs:
        //                case Type2Operator2.add:
        //                case Type2Operator2.sub:
        //                case Type2Operator2.div:
        //                case Type2Operator2.neg:
        //                case Type2Operator2.random:
        //                case Type2Operator2.mul:
        //                case Type2Operator2.sqrt:
        //                case Type2Operator2.drop:
        //                case Type2Operator2.exch:
        //                case Type2Operator2.index:
        //                case Type2Operator2.roll:
        //                case Type2Operator2.dup:

        public void Op_Abs()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Op_Abs));
        }
        public void Op_Add()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Op_Add));
        }
        public void Op_Sub()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Op_Sub));
        }
        public void Op_Div()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Op_Div));
        }
        public void Op_Neg()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Op_Neg));
        }
        public void Op_Random()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Op_Random));
        }
        public void Op_Mul()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Op_Mul));
        }
        public void Op_Sqrt()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Op_Sqrt));
        }
        public void Op_Drop()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Op_Drop));
        }
        public void Op_Exch()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Op_Exch));
        }
        public void Op_Index()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Op_Index));
        }
        public void Op_Roll()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Op_Roll));
        }
        public void Op_Dup()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Op_Dup));
        }


        //-------------------------
        //4.5: Storage Operators

        //The storage operators utilize a transient array and provide 
        //facilities for storing and retrieving transient array data. 

        //The transient array provides non-persistent storage for 
        //intermediate values. 
        //There is no provision to initialize this array, 
        //except explicitly using the put operator, 
        //and values stored in the 
        //array do not persist beyond the scope of rendering an individual 
        //character. 

        public void Put()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Put));
        }
        public void Get()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Get));
        }

        //-------------------------
        //4.6: Conditional  
        public void Op_And()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Op_And));
        }
        public void Op_Or()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Op_Or));
        }
        public void Op_Not()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Op_Not));
        }
        public void Op_Eq()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Op_Eq));
        }
        public void Op_IfElse()
        {
            Debug.WriteLine("NOT_IMPLEMENT:" + nameof(Op_IfElse));
        }
        public double Pop()
        {
#if DEBUG
            if (_currentIndex < 1)
            {

            }
#endif
            return (double)_argStack[--_currentIndex];//*** use prefix 
        }

        public void Ret()
        {
#if DEBUG
            if (_currentIndex > 0)
            {

            }
#endif
            _currentIndex = 0;
        }
#if DEBUG
        public void dbugClearEvalStack()
        {
            _currentIndex = 0;
        }
#endif
    }




}
