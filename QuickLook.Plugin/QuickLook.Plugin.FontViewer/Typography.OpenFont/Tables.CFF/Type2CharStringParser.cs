//Apache2, 2018, Villu Ruusmann , Apache/PdfBox Authors ( https://github.com/apache/pdfbox)  
//Apache2, 2018-present, WinterDev 

//ref http://wwwimages.adobe.com/www.adobe.com/content/dam/acom/en/devnet/font/pdfs/5177.Type2.pdf

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Typography.OpenFont.CFF
{


    //The Type 2 Charstring Format
    //...
    //must be used in a CFF (Compact Font Format) or OpenType font 
    //file to create a complete font program

    readonly struct Type2Instruction
    {
        public readonly int Value;
        public readonly byte Op;
        public Type2Instruction(OperatorName op, int value)
        {
            this.Op = (byte)op;
            this.Value = value;
#if DEBUG
            _dbug_OnlyOp = false;
#endif
        }
        public Type2Instruction(byte op, int value)
        {
            this.Op = op;
            this.Value = value;
#if DEBUG
            _dbug_OnlyOp = false;
#endif
        }
        public Type2Instruction(OperatorName op)
        {
            this.Op = (byte)op;
            this.Value = 0;
#if DEBUG
            _dbug_OnlyOp = true;
#endif
        }


        public float ReadValueAsFixed1616()
        {
            byte b0 = (byte)((0xff) & Value >> 24);
            byte b1 = (byte)((0xff) & Value >> 16);
            byte b2 = (byte)((0xff) & Value >> 8);
            byte b3 = (byte)((0xff) & Value >> 0);


            ///This number is interpreted as a Fixed; that is, a signed number with 16 bits of fraction
            float int_part = (short)((b0 << 8) | b1);
            float fraction_part = (short)((b2 << 8) | b3) / (float)(1 << 16);
            return int_part + fraction_part;
        }

        internal bool IsLoadInt => (OperatorName)Op == OperatorName.LoadInt;

#if DEBUG
        readonly bool _dbug_OnlyOp;

        [System.ThreadStatic]
        static System.Text.StringBuilder s_dbugSb;

        public override string ToString()
        {

            int merge_flags = Op >> 6; //upper most 2 bits we use as our extension
            //so operator name is lower 6 bits

            int only_operator = Op & 0b111111;
            OperatorName op_name = (OperatorName)only_operator;

            if (_dbug_OnlyOp)
            {
                return op_name.ToString();
            }
            else
            {
                if (s_dbugSb == null)
                {
                    s_dbugSb = new System.Text.StringBuilder();
                }
                s_dbugSb.Length = 0;//reset

                bool has_ExtenedForm = true;


                //this is my extension
                switch (merge_flags)
                {
#if DEBUG
                    default: throw new OpenFontNotSupportedException();
#endif
                    case 0:
                        //nothing 
                        has_ExtenedForm = false;
                        break;
                    case 1:
                        //contains merge data for LoadInt
                        s_dbugSb.Append(Value.ToString() + " ");
                        break;
                    case 2:
                        //contains merge data for LoadShort2
                        s_dbugSb.Append((short)(Value >> 16) + " " + (short)(Value >> 0) + " ");
                        break;
                    case 3:
                        //contains merge data for LoadSbyte4
                        s_dbugSb.Append((sbyte)(Value >> 24) + " " + (sbyte)(Value >> 16) + " " + (sbyte)(Value >> 8) + " " + (sbyte)(Value) + " ");
                        break;
                }

                switch (op_name)
                {
                    case OperatorName.LoadInt:
                        s_dbugSb.Append(Value);
                        break;
                    case OperatorName.LoadFloat:
                        s_dbugSb.Append(ReadValueAsFixed1616().ToString());
                        break;
                    //-----------
                    case OperatorName.LoadShort2:
                        s_dbugSb.Append((short)(Value >> 16) + " " + (short)(Value >> 0));
                        break;
                    case OperatorName.LoadSbyte4:
                        s_dbugSb.Append((sbyte)(Value >> 24) + " " + (sbyte)(Value >> 16) + " " + (sbyte)(Value >> 8) + " " + (sbyte)(Value));
                        break;
                    case OperatorName.LoadSbyte3:
                        s_dbugSb.Append((sbyte)(Value >> 24) + " " + (sbyte)(Value >> 16) + " " + (sbyte)(Value >> 8));
                        break;
                    //-----------     
                    case OperatorName.hintmask1:
                    case OperatorName.hintmask2:
                    case OperatorName.hintmask3:
                    case OperatorName.hintmask4:
                    case OperatorName.hintmask_bits:
                        s_dbugSb.Append((op_name).ToString() + " " + Convert.ToString(Value, 2));
                        break;
                    default:
                        if (has_ExtenedForm)
                        {
                            s_dbugSb.Append((op_name).ToString());
                        }
                        else
                        {
                            s_dbugSb.Append((op_name).ToString() + " " + Value.ToString());
                        }

                        break;
                }

                return s_dbugSb.ToString();

            }

        }
#endif
    }


    class OriginalType2OperatorAttribute : Attribute
    {
        public OriginalType2OperatorAttribute(Type2Operator1 type2Operator1)
        {
        }
        public OriginalType2OperatorAttribute(Type2Operator2 type2Operator1)
        {
        }
    }
    enum Type2Operator1 : byte
    {
        //Appendix A Type 2 Charstring Command Codes       
        _Reserved0_ = 0,
        hstem, //1
        _Reserved2_,//2
        vstem, //3
        vmoveto,//4
        rlineto, //5
        hlineto, //6
        vlineto,//7,
        rrcurveto,//8
        _Reserved9_, //9
        callsubr, //10
                  //---------------------
        _return, //11
        escape,//12
        _Reserved13_,
        endchar,//14
        _Reserved15_,
        _Reserved16_,
        _Reserved17_,
        hstemhm,//18
        hintmask,//19
        cntrmask,//20
                 //---------------------
        rmoveto,//21
        hmoveto,//22
        vstemhm,//23
        rcurveline, //24
        rlinecurve,//25
        vvcurveto,//26
        hhcurveto, //27
        shortint, //28
        callgsubr, //29
        vhcurveto, //30
                   //-----------------------
        hvcurveto, //31
    }
    enum Type2Operator2 : byte
    {
        //Two-byte Type 2 Operators
        _Reserved0_ = 0,
        _Reserved1_,
        _Reserved2_,
        and, //3
        or, //4
        not, //5
        _Reserved6_,
        _Reserved7_,
        _Reserved8_,
        //
        abs,//9        
        add,//10
            //------------------
        sub,//11
        div,//12
        _Reserved13_,
        neg,//14
        eq, //15
        _Reserved16_,
        _Reserved17_,
        drop,//18
        _Reserved19_,
        put,//20
            //------------------ 
        get, //21
        ifelse,//22
        random,//23
        mul, //24,
        _Reserved25_,
        sqrt,//26
        dup,//27
        exch,//28 , exchanges the top two elements on the argument stack
        index,//29
        roll,//30
             //--------------
        _Reserved31_,
        _Reserved32_,
        _Reserved33_,
        //--------------
        hflex,//34
        flex, //35
        hflex1,//36
        flex1//37
    }

    /// <summary>
    /// Merged ccf operators,(op1 and op2, note on attribute of each field)
    /// </summary>
    enum OperatorName : byte
    {
        Unknown,
        //
        LoadInt,
        LoadFloat,
        GlyphWidth,

        LoadSbyte4, //my extension, 4 sbyte in an int32
        LoadSbyte3, //my extension, 3 sbytes in an int32
        LoadShort2, //my extension, 2 short in an int32

        //---------------------
        //type2Operator1
        //---------------------
        [OriginalType2Operator(Type2Operator1.hstem)] hstem,
        [OriginalType2Operator(Type2Operator1.vstem)] vstem,
        [OriginalType2Operator(Type2Operator1.vmoveto)] vmoveto,
        [OriginalType2Operator(Type2Operator1.rlineto)] rlineto,
        [OriginalType2Operator(Type2Operator1.hlineto)] hlineto,
        [OriginalType2Operator(Type2Operator1.vlineto)] vlineto,
        [OriginalType2Operator(Type2Operator1.rrcurveto)] rrcurveto,
        [OriginalType2Operator(Type2Operator1.callsubr)] callsubr,
        //---------------------
        [OriginalType2Operator(Type2Operator1._return)] _return,
        //[OriginalType2Operator(Type2Operator1.escape)] escape, //not used!
        [OriginalType2Operator(Type2Operator1.endchar)] endchar,
        [OriginalType2Operator(Type2Operator1.hstemhm)] hstemhm,

        //---------
        [OriginalType2Operator(Type2Operator1.hintmask)] hintmask1, //my hint-mask extension, contains 1 byte hint
        [OriginalType2Operator(Type2Operator1.hintmask)] hintmask2, //my hint-mask extension, contains 2 bytes hint
        [OriginalType2Operator(Type2Operator1.hintmask)] hintmask3, //my hint-mask extension, contains 3 bytes hint
        [OriginalType2Operator(Type2Operator1.hintmask)] hintmask4, //my hint-mask extension, contains 4 bytes hint 
        [OriginalType2Operator(Type2Operator1.hintmask)] hintmask_bits,//my hint-mask extension, contains n bits of hint

        //---------

        [OriginalType2Operator(Type2Operator1.cntrmask)] cntrmask1, //my counter-mask extension, contains 1 byte hint
        [OriginalType2Operator(Type2Operator1.cntrmask)] cntrmask2, //my counter-mask extension, contains 2 bytes hint
        [OriginalType2Operator(Type2Operator1.cntrmask)] cntrmask3, //my counter-mask extension, contains 3 bytes hint
        [OriginalType2Operator(Type2Operator1.cntrmask)] cntrmask4, //my counter-mask extension, contains 4 bytes hint
        [OriginalType2Operator(Type2Operator1.cntrmask)] cntrmask_bits, //my counter-mask extension, contains n bits of hint

        //---------------------
        [OriginalType2Operator(Type2Operator1.rmoveto)] rmoveto,
        [OriginalType2Operator(Type2Operator1.hmoveto)] hmoveto,
        [OriginalType2Operator(Type2Operator1.vstemhm)] vstemhm,
        [OriginalType2Operator(Type2Operator1.rcurveline)] rcurveline,
        [OriginalType2Operator(Type2Operator1.rlinecurve)] rlinecurve,
        [OriginalType2Operator(Type2Operator1.vvcurveto)] vvcurveto,
        [OriginalType2Operator(Type2Operator1.hhcurveto)] hhcurveto,
        [OriginalType2Operator(Type2Operator1.shortint)] shortint,
        [OriginalType2Operator(Type2Operator1.callgsubr)] callgsubr,
        [OriginalType2Operator(Type2Operator1.vhcurveto)] vhcurveto,
        //-----------------------
        [OriginalType2Operator(Type2Operator1.hvcurveto)] hvcurveto,
        //--------------------- 
        //Two-byte Type 2 Operators 
        [OriginalType2Operator(Type2Operator2.and)] and,
        [OriginalType2Operator(Type2Operator2.or)] or,
        [OriginalType2Operator(Type2Operator2.not)] not,
        [OriginalType2Operator(Type2Operator2.abs)] abs,
        [OriginalType2Operator(Type2Operator2.add)] add,
        //------------------
        [OriginalType2Operator(Type2Operator2.sub)] sub,
        [OriginalType2Operator(Type2Operator2.div)] div,
        [OriginalType2Operator(Type2Operator2.neg)] neg,
        [OriginalType2Operator(Type2Operator2.eq)] eq,
        [OriginalType2Operator(Type2Operator2.drop)] drop,
        [OriginalType2Operator(Type2Operator2.put)] put,
        //------------------ 
        [OriginalType2Operator(Type2Operator2.get)] get,
        [OriginalType2Operator(Type2Operator2.ifelse)] ifelse,
        [OriginalType2Operator(Type2Operator2.random)] random,
        [OriginalType2Operator(Type2Operator2.mul)] mul,
        [OriginalType2Operator(Type2Operator2.sqrt)] sqrt,
        [OriginalType2Operator(Type2Operator2.dup)] dup,
        [OriginalType2Operator(Type2Operator2.exch)] exch,
        [OriginalType2Operator(Type2Operator2.index)] index,
        [OriginalType2Operator(Type2Operator2.roll)] roll,
        [OriginalType2Operator(Type2Operator2.hflex)] hflex,
        [OriginalType2Operator(Type2Operator2.flex)] flex,
        [OriginalType2Operator(Type2Operator2.hflex1)] hflex1,
        [OriginalType2Operator(Type2Operator2.flex1)] flex1
    }



    class Type2GlyphInstructionList
    {
        List<Type2Instruction> _insts;

        public Type2GlyphInstructionList()
        {
            _insts = new List<Type2Instruction>();
        }

        public Type2Instruction RemoveLast()
        {
            int last = _insts.Count - 1;
            Type2Instruction _lastInst = _insts[last];
            _insts.RemoveAt(last);
            return _lastInst;
        }
        //
        public void AddInt(int intValue)
        {
#if DEBUG
            debugCheck();
#endif
            _insts.Add(new Type2Instruction(OperatorName.LoadInt, intValue));
        }
        public void AddFloat(int float1616Fmt)
        {
#if DEBUG
            debugCheck();
            //var test = new Type2Instruction(OperatorName.LoadFloat, float1616Fmt);
            //string str = test.ToString();
#endif 
            _insts.Add(new Type2Instruction(OperatorName.LoadFloat, float1616Fmt));
        }
        public void AddOp(OperatorName opName)
        {
#if DEBUG
            debugCheck();
#endif
            _insts.Add(new Type2Instruction(opName));
        }

        public void AddOp(OperatorName opName, int value)
        {
#if DEBUG
            debugCheck();
#endif
            _insts.Add(new Type2Instruction(opName, value));
        }
        public int Count => _insts.Count;
        internal void ChangeFirstInstToGlyphWidthValue()
        {
            //check the first element must be loadint
            if (_insts.Count == 0) return;

            Type2Instruction firstInst = _insts[0];
            if (!firstInst.IsLoadInt) { throw new OpenFontNotSupportedException(); }
            //the replace
            _insts[0] = new Type2Instruction(OperatorName.GlyphWidth, firstInst.Value);
        }




        internal List<Type2Instruction> InnerInsts => _insts;

#if DEBUG
        void debugCheck()
        {
            if (_dbugMark == 5 && _insts.Count > 50)
            {

            }
        }
        public int dbugInstCount => _insts.Count;
        int _dbugMark;

        public ushort dbugGlyphIndex;

        public int dbugMark
        {
            get => _dbugMark;
            set
            {
                _dbugMark = value;
            }
        }

        public void dbugDumpInstructionListToFile(string filename)
        {
            dbugCffInstHelper.dbugDumpInstructionListToFile(_insts, filename);
        }
#endif
    }

#if DEBUG
    public static class dbugCffInstHelper
    {
        internal static void dbugDumpInstructionListToFile(this IEnumerable<Type2Instruction> insts, string filename)
        {
            using (FileStream fs = new FileStream(filename, FileMode.Create))
            using (StreamWriter w = new StreamWriter(fs))
            {
                int i = 0;
                foreach (Type2Instruction inst in insts)
                {

                    w.Write("[" + i + "] ");
                    if (inst.IsLoadInt)
                    {
                        w.Write(inst.Value.ToString());
                        w.Write(' ');
                    }
                    else
                    {
                        w.Write(inst.ToString());
                        w.WriteLine();
                    }
                    i++;
                }
            }
        }
    }

#endif


    class Type2CharStringParser
    {
        //from https://www.adobe.com/content/dam/acom/en/devnet/font/pdfs/5177.Type2.pdf
        //or https://adobe-type-tools.github.io/font-tech-notes/pdfs/5177.Type2.pdf

        //Type 2 Charstring Organization:
        //...
        //The sequence and form of a Type 2 charstring program may be represented as:

        //w? {hs* vs* cm* hm* mt subpath}? {mt subpath}* endchar

        //where,
        //w= width,
        //hs = hstem or hstemhm command
        //vs = vstem or vstemhm command
        //cm = cntrmask operator
        //hm = hintmask operator
        //mt = moveto (i.e.any of the moveto) operators

        //subpath = refers to the construction of a subpath(one complete closed contour),
        // 	        which may include hintmaskoperators where appropriate.

        //-------------
        //
        //width: If the charstring has a width other than that of defaultWidthX(see Technical Note #5176, “The Compact Font Format Specification”),
        // it must be specified as the first number in the charstring,
        //and encoded as the difference from nominalWidthX


        public Type2CharStringParser()
        {
        }

#if DEBUG
        int _dbugCount = 0;
        int _dbugInstructionListMark = 0;
#endif 
        int _hintStemCount = 0;
        bool _foundSomeStem = false;
        bool _enterPathConstructionSeq = false;

        Type2GlyphInstructionList _insts;
        int _current_integer_count = 0;
        bool _doStemCount = true;
        Cff1Font _currentCff1Font;
        int _globalSubrBias;
        int _localSubrBias;

        public void SetCurrentCff1Font(Cff1Font currentCff1Font)
        {
            //this will provide subr buffer for callsubr callgsubr
            _currentFontDict = null;//reset
            _currentCff1Font = currentCff1Font;

            if (_currentCff1Font._globalSubrRawBufferList != null)
            {
                _globalSubrBias = CalculateBias(currentCff1Font._globalSubrRawBufferList.Count);
            }
            if (_currentCff1Font._localSubrRawBufferList != null)
            {
                _localSubrBias = CalculateBias(currentCff1Font._localSubrRawBufferList.Count);
            }
        }


        static int CalculateBias(int nsubr)
        {
            //-------------
            //from Technical Note #5176 (CFF spec)
            //resolve with bias
            //Card16 bias;
            //Card16 nSubrs = subrINDEX.count;
            //if (CharstringType == 1)
            //    bias = 0;
            //else if (nSubrs < 1240)
            //    bias = 107;
            //else if (nSubrs < 33900)
            //    bias = 1131;
            //else
            //    bias = 32768; 
            //find local subroutine 
            return (nsubr < 1240) ? 107 : (nsubr < 33900) ? 1131 : 32768;
        }

        struct SimpleBinaryReader
        {
            byte[] _buffer;
            int _pos;
            public SimpleBinaryReader(byte[] buffer)
            {
                _buffer = buffer;
                _pos = 0;
            }
            public bool IsEnd() => _pos >= _buffer.Length;
            public byte ReadByte()
            {
                //read current byte to stack and advance pos after read 
                return _buffer[_pos++];
            }
            public int BufferLength => _buffer.Length;
            public int Position => _pos;

            public int ReadFloatFixed1616()
            {
                byte b0 = _buffer[_pos];
                byte b1 = _buffer[_pos + 1];
                byte b2 = _buffer[_pos + 2];
                byte b3 = _buffer[_pos + 3];

                _pos += 4;
                return (b0 << 24) | (b1 << 16) | (b2 << 8) | b3;
            }
        }

        void ParseType2CharStringBuffer(byte[] buffer)
        {
            byte b0 = 0;

            bool cont = true;

            var reader = new SimpleBinaryReader(buffer);
            while (cont && !reader.IsEnd())
            {
                b0 = reader.ReadByte();
#if DEBUG
                //easy for debugging here
                _dbugCount++;
                if (b0 < 32)
                {

                }
#endif
                switch (b0)
                {
                    default: //else 32 -255
                        {
                            if (b0 < 32)
                            {
                                Debug.WriteLine("err!:" + b0);
                                return;
                            }
                            //
                            _insts.AddInt(ReadIntegerNumber(ref reader, b0));
                            if (_doStemCount)
                            {
                                _current_integer_count++;
                            }
                        }
                        break;
                    case 255:
                        {

                            //from https://www.adobe.com/content/dam/acom/en/devnet/font/pdfs/5177.Type2.pdf
                            //If the charstring byte contains the value 255,
                            //the next four bytes indicate a two’s complement signed number.

                            //The first of these four bytes contains the highest order bits,
                            //he second byte contains the next higher order bits and
                            //the fourth byte contains the lowest order bits.

                            //eg. found in font Asana Math regular, glyph_index: 114 , 292, 1070 etc.

                            _insts.AddFloat(reader.ReadFloatFixed1616());

                            if (_doStemCount)
                            {
                                _current_integer_count++;
                            }
                        }
                        break;
                    case (byte)Type2Operator1.shortint: // 28

                        //shortint
                        //First byte of a 3-byte sequence specifying a number.
                        //a ShortInt value is specified by using the operator (28) followed by two bytes
                        //which represent numbers between –32768 and + 32767.The
                        //most significant byte follows the(28)
                        byte s_b0 = reader.ReadByte();
                        byte s_b1 = reader.ReadByte();
                        _insts.AddInt((short)((s_b0 << 8) | (s_b1)));
                        //
                        if (_doStemCount)
                        {
                            _current_integer_count++;
                        }
                        break;
                    //---------------------------------------------------
                    case (byte)Type2Operator1._Reserved0_://???
                    case (byte)Type2Operator1._Reserved2_://???
                    case (byte)Type2Operator1._Reserved9_://???
                    case (byte)Type2Operator1._Reserved13_://???
                    case (byte)Type2Operator1._Reserved15_://???
                    case (byte)Type2Operator1._Reserved16_: //???
                    case (byte)Type2Operator1._Reserved17_: //???
                        //reserved, do nothing ?
                        break;

                    case (byte)Type2Operator1.escape: //12
                        {

                            b0 = reader.ReadByte();
                            switch ((Type2Operator2)b0)
                            {
                                default:
                                    if (b0 <= 38)
                                    {
                                        Debug.WriteLine("err!:" + b0);
                                        return;
                                    }
                                    break;
                                //-------------------------
                                //4.1: Path Construction Operators
                                case Type2Operator2.flex: _insts.AddOp(OperatorName.flex); break;
                                case Type2Operator2.hflex: _insts.AddOp(OperatorName.hflex); break;
                                case Type2Operator2.hflex1: _insts.AddOp(OperatorName.hflex1); break;
                                case Type2Operator2.flex1: _insts.AddOp(OperatorName.flex1); ; break;
                                //-------------------------
                                //4.4: Arithmetic Operators
                                case Type2Operator2.abs: _insts.AddOp(OperatorName.abs); break;
                                case Type2Operator2.add: _insts.AddOp(OperatorName.add); break;
                                case Type2Operator2.sub: _insts.AddOp(OperatorName.sub); break;
                                case Type2Operator2.div: _insts.AddOp(OperatorName.div); break;
                                case Type2Operator2.neg: _insts.AddOp(OperatorName.neg); break;
                                case Type2Operator2.random: _insts.AddOp(OperatorName.random); break;
                                case Type2Operator2.mul: _insts.AddOp(OperatorName.mul); break;
                                case Type2Operator2.sqrt: _insts.AddOp(OperatorName.sqrt); break;
                                case Type2Operator2.drop: _insts.AddOp(OperatorName.drop); break;
                                case Type2Operator2.exch: _insts.AddOp(OperatorName.exch); break;
                                case Type2Operator2.index: _insts.AddOp(OperatorName.index); break;
                                case Type2Operator2.roll: _insts.AddOp(OperatorName.roll); break;
                                case Type2Operator2.dup: _insts.AddOp(OperatorName.dup); break;

                                //-------------------------
                                //4.5: Storage Operators 
                                case Type2Operator2.put: _insts.AddOp(OperatorName.put); break;
                                case Type2Operator2.get: _insts.AddOp(OperatorName.get); break;
                                //-------------------------
                                //4.6: Conditional
                                case Type2Operator2.and: _insts.AddOp(OperatorName.and); break;
                                case Type2Operator2.or: _insts.AddOp(OperatorName.or); break;
                                case Type2Operator2.not: _insts.AddOp(OperatorName.not); break;
                                case Type2Operator2.eq: _insts.AddOp(OperatorName.eq); break;
                                case Type2Operator2.ifelse: _insts.AddOp(OperatorName.ifelse); break;
                            }

                            StopStemCount();
                        }
                        break;

                    //---------------------------------------------------------------------------
                    case (byte)Type2Operator1.endchar:
                        AddEndCharOp();
                        cont = false;
                        //when we found end char
                        //stop reading this...
                        break;
                    case (byte)Type2Operator1.rmoveto: AddMoveToOp(OperatorName.rmoveto); StopStemCount(); break;
                    case (byte)Type2Operator1.hmoveto: AddMoveToOp(OperatorName.hmoveto); StopStemCount(); break;
                    case (byte)Type2Operator1.vmoveto: AddMoveToOp(OperatorName.vmoveto); StopStemCount(); break;
                    //---------------------------------------------------------------------------
                    case (byte)Type2Operator1.rlineto: _insts.AddOp(OperatorName.rlineto); StopStemCount(); break;
                    case (byte)Type2Operator1.hlineto: _insts.AddOp(OperatorName.hlineto); StopStemCount(); break;
                    case (byte)Type2Operator1.vlineto: _insts.AddOp(OperatorName.vlineto); StopStemCount(); break;
                    case (byte)Type2Operator1.rrcurveto: _insts.AddOp(OperatorName.rrcurveto); StopStemCount(); break;
                    case (byte)Type2Operator1.hhcurveto: _insts.AddOp(OperatorName.hhcurveto); StopStemCount(); break;
                    case (byte)Type2Operator1.hvcurveto: _insts.AddOp(OperatorName.hvcurveto); StopStemCount(); break;
                    case (byte)Type2Operator1.rcurveline: _insts.AddOp(OperatorName.rcurveline); StopStemCount(); break;
                    case (byte)Type2Operator1.rlinecurve: _insts.AddOp(OperatorName.rlinecurve); StopStemCount(); break;
                    case (byte)Type2Operator1.vhcurveto: _insts.AddOp(OperatorName.vhcurveto); StopStemCount(); break;
                    case (byte)Type2Operator1.vvcurveto: _insts.AddOp(OperatorName.vvcurveto); StopStemCount(); break;
                    //-------------------------------------------------------------------
                    //4.3 Hint Operators
                    case (byte)Type2Operator1.hstem: AddStemToList(OperatorName.hstem); break;
                    case (byte)Type2Operator1.vstem: AddStemToList(OperatorName.vstem); break;
                    case (byte)Type2Operator1.vstemhm: AddStemToList(OperatorName.vstemhm); break;
                    case (byte)Type2Operator1.hstemhm: AddStemToList(OperatorName.hstemhm); break;
                    //-------------------------------------------------------------------
                    case (byte)Type2Operator1.hintmask: AddHintMaskToList(ref reader); StopStemCount(); break;
                    case (byte)Type2Operator1.cntrmask: AddCounterMaskToList(ref reader); StopStemCount(); break;
                    //-------------------------------------------------------------------
                    //4.7: Subroutine Operators                   
                    case (byte)Type2Operator1._return:
                        {
#if DEBUG
                            if (!reader.IsEnd())
                            {
                                throw new OpenFontNotSupportedException();
                            }

#endif
                        }
                        return;
                    //-------------------------------------------------------------------
                    case (byte)Type2Operator1.callsubr:
                        {
                            //get local subr proc
                            if (_currentCff1Font != null)
                            {
                                Type2Instruction inst = _insts.RemoveLast();
                                if (!inst.IsLoadInt)
                                {
                                    throw new OpenFontNotSupportedException();
                                }
                                if (_doStemCount)
                                {
                                    _current_integer_count--;
                                }
                                //subr_no must be adjusted with proper bias value 
                                if (_currentCff1Font._localSubrRawBufferList != null)
                                {
                                    ParseType2CharStringBuffer(_currentCff1Font._localSubrRawBufferList[inst.Value + _localSubrBias]);
                                }
                                else if (_currentFontDict != null)
                                {
                                    //use private dict
                                    ParseType2CharStringBuffer(_currentFontDict.LocalSubr[inst.Value + _localSubrBias]);
                                }
                                else
                                {
                                    throw new OpenFontNotSupportedException();
                                }
                            }
                        }
                        break;
                    case (byte)Type2Operator1.callgsubr:
                        {
                            if (_currentCff1Font != null)
                            {
                                Type2Instruction inst = _insts.RemoveLast();
                                if (!inst.IsLoadInt)
                                {
                                    throw new OpenFontNotSupportedException();
                                }
                                if (_doStemCount)
                                {
                                    _current_integer_count--;
                                }
                                //subr_no must be adjusted with proper bias value 
                                //load global subr
                                ParseType2CharStringBuffer(_currentCff1Font._globalSubrRawBufferList[inst.Value + _globalSubrBias]);
                            }
                        }
                        break;
                }
            }
        }
#if DEBUG
        public ushort dbugCurrentGlyphIndex;
#endif
        FontDict _currentFontDict;
        public void SetCidFontDict(FontDict fontdic)
        {
#if DEBUG
            if (fontdic == null)
            {
                throw new OpenFontNotSupportedException();
            }
#endif

            _currentFontDict = fontdic;
            if (fontdic.LocalSubr != null)
            {
                _localSubrBias = CalculateBias(_currentFontDict.LocalSubr.Count);
            }
            else
            {
                _localSubrBias = 0;
            }
        }

        public Type2GlyphInstructionList ParseType2CharString(byte[] buffer)
        {
            //reset
            _hintStemCount = 0;
            _current_integer_count = 0;
            _foundSomeStem = false;
            _enterPathConstructionSeq = false;
            _doStemCount = true;

            _insts = new Type2GlyphInstructionList();
            //--------------------
#if DEBUG
            _dbugInstructionListMark++;
            if (_currentCff1Font == null)
            {
                throw new OpenFontNotSupportedException();
            }
            //
            _insts.dbugGlyphIndex = dbugCurrentGlyphIndex;

            if (dbugCurrentGlyphIndex == 496)
            {

            }
#endif
            ParseType2CharStringBuffer(buffer);

#if DEBUG
            if (dbugCurrentGlyphIndex == 496)
            {
                //_insts.dbugDumpInstructionListToFile("glyph_496.txt");
            }
#endif
            return _insts;
        }

        void StopStemCount()
        {
            _current_integer_count = 0;
            _doStemCount = false;
        }
        OperatorName _latestOpName = OperatorName.Unknown;

        void AddEndCharOp()
        {
            //from https://www.adobe.com/content/dam/acom/en/devnet/font/pdfs/5177.Type2.pdf
            //Note 4 The first stack - clearing operator, which must be one of
            //hstem, hstemhm, vstem, vstemhm, 
            //cntrmask, hintmask, 
            //hmoveto, vmoveto, rmoveto,
            //or endchar,
            //takes an additional argument — the width(as described earlier), which may be expressed as zero or one numeric argument

            if (!_foundSomeStem && !_enterPathConstructionSeq)
            {
                if (_insts.Count > 0)
                {
                    _insts.ChangeFirstInstToGlyphWidthValue();
                }
            }
            //takes an additional argument — the width(as described earlier), which may be expressed as zero or one numeric argument
            _insts.AddOp(OperatorName.endchar);
        }



        /// <summary>
        /// for hmoveto, vmoveto, rmoveto
        /// </summary>
        /// <param name="op"></param>
        void AddMoveToOp(OperatorName op)
        {
            //from https://www.adobe.com/content/dam/acom/en/devnet/font/pdfs/5177.Type2.pdf
            //Note 4 The first stack - clearing operator, which must be one of
            //hstem, hstemhm, vstem, vstemhm, 
            //cntrmask, hintmask, 
            //hmoveto, vmoveto, rmoveto,
            //or endchar,
            //takes an additional argument — the width(as described earlier), which may be expressed as zero or one numeric argument 
            //just add

            if (!_foundSomeStem && !_enterPathConstructionSeq)
            {
                if (op == OperatorName.rmoveto)
                {
                    if ((_insts.Count % 2) != 0)
                    {
                        _insts.ChangeFirstInstToGlyphWidthValue();
                    }
                }
                else
                {
                    //vmoveto, hmoveto
                    if (_insts.Count > 1)
                    {
                        //...
                        _insts.ChangeFirstInstToGlyphWidthValue();
                    }
                }
            }
            _enterPathConstructionSeq = true;
            _insts.AddOp(op);
        }
        /// <summary>
        /// for hstem, hstemhm, vstem, vstemhm
        /// </summary>
        /// <param name="stemName"></param>
        void AddStemToList(OperatorName stemName)
        {

            //from https://www.adobe.com/content/dam/acom/en/devnet/font/pdfs/5177.Type2.pdf
            //Note 4 The first stack - clearing operator, which must be one of
            //hstem, hstemhm, vstem, vstemhm, 
            //cntrmask, hintmask, 
            //hmoveto, vmoveto, rmoveto,
            //or endchar,
            //takes an additional argument — the width(as described earlier), which may be expressed as zero or one numeric argument

            //support 4 kinds 

            //1. 
            //|- y dy {dya dyb}*  hstemhm (18) |-
            //2.
            //|- x dx {dxa dxb}* vstemhm (23) |-
            //3.
            //|- y dy {dya dyb}*  hstem (1) |-
            //4. 
            //|- x dx {dxa dxb}*  vstem (3) |- 
            //-----------------------

            //notes
            //The sequence and form of a Type 2 charstring program may be
            //represented as:
            //w? { hs* vs*cm * hm * mt subpath}? { mt subpath} *endchar 

            if ((_current_integer_count % 2) != 0)
            {
                //all kind has even number of stem
                if (_foundSomeStem)
                {
#if DEBUG
                    _insts.dbugDumpInstructionListToFile("test_type2_" + (_dbugInstructionListMark - 1) + ".txt");
#endif
                    throw new OpenFontNotSupportedException();
                }
                else
                {
                    //the first one is 'width'
                    _insts.ChangeFirstInstToGlyphWidthValue();
                    _current_integer_count--;
                }
            }
            _hintStemCount += (_current_integer_count / 2); //save a snapshot of stem count
            _insts.AddOp(stemName);
            _current_integer_count = 0;//clear
            _foundSomeStem = true;
            _latestOpName = stemName;
        }
        /// <summary>
        /// add hintmask
        /// </summary>
        /// <param name="reader"></param>
        void AddHintMaskToList(ref SimpleBinaryReader reader)
        {
            if (_foundSomeStem && _current_integer_count > 0)
            {

                //type2 5177.pdf
                //...
                //If hstem and vstem hints are both declared at the beginning of
                //a charstring, and this sequence is followed directly by the
                //hintmask or cntrmask operators, ...
                //the vstem hint operator need not be included ***

#if DEBUG
                if ((_current_integer_count % 2) != 0)
                {
                    throw new OpenFontNotSupportedException();
                }
                else
                {

                }
#endif
                if (_doStemCount)
                {
                    switch (_latestOpName)
                    {
                        case OperatorName.hstem:
                            //add vstem  ***( from reason above)

                            _hintStemCount += (_current_integer_count / 2); //save a snapshot of stem count
                            _insts.AddOp(OperatorName.vstem);
                            _latestOpName = OperatorName.vstem;
                            _current_integer_count = 0; //clear
                            break;
                        case OperatorName.hstemhm:
                            //add vstem  ***( from reason above) ??
                            _hintStemCount += (_current_integer_count / 2); //save a snapshot of stem count
                            _insts.AddOp(OperatorName.vstem);
                            _latestOpName = OperatorName.vstem;
                            _current_integer_count = 0;//clear
                            break;
                        case OperatorName.vstemhm:
                            //-------
                            //TODO: review here? 
                            //found this in xits.otf
                            _hintStemCount += (_current_integer_count / 2); //save a snapshot of stem count
                            _insts.AddOp(OperatorName.vstem);
                            _latestOpName = OperatorName.vstem;
                            _current_integer_count = 0;//clear
                            break;
                        default:
                            throw new OpenFontNotSupportedException();
                    }
                }
                else
                {

                }
            }

            if (_hintStemCount == 0)
            {
                if (!_foundSomeStem)
                {
                    _hintStemCount = (_current_integer_count / 2);
                    if (_hintStemCount == 0)
                    {
                        return;
                    }
                    _foundSomeStem = true;//?
                }
                else
                {
                    throw new OpenFontNotSupportedException();
                }
            }

            //---------------------- 
            //this is my hintmask extension, => to fit with our Evaluation stack
            int properNumberOfMaskBytes = (_hintStemCount + 7) / 8;

            if (reader.Position + properNumberOfMaskBytes >= reader.BufferLength)
            {
                throw new OpenFontNotSupportedException();
            }
            if (properNumberOfMaskBytes > 4)
            {
                int remaining = properNumberOfMaskBytes;

                for (; remaining > 3;)
                {
                    _insts.AddInt((
                       (reader.ReadByte() << 24) |
                       (reader.ReadByte() << 16) |
                       (reader.ReadByte() << 8) |
                       (reader.ReadByte())
                       ));
                    remaining -= 4; //*** 
                }
                switch (remaining)
                {
                    case 0:
                        //do nothing
                        break;
                    case 1:
                        _insts.AddInt(reader.ReadByte() << 24);
                        break;
                    case 2:
                        _insts.AddInt(
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16));

                        break;
                    case 3:
                        _insts.AddInt(
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16) |
                            (reader.ReadByte() << 8));
                        break;
                    default: throw new OpenFontNotSupportedException();//should not occur !
                }

                _insts.AddOp(OperatorName.hintmask_bits, properNumberOfMaskBytes);
            }
            else
            {
                //last remaining <4 bytes 
                switch (properNumberOfMaskBytes)
                {
                    case 0:
                    default: throw new OpenFontNotSupportedException();//should not occur !                     
                    case 1:
                        _insts.AddOp(OperatorName.hintmask1, (reader.ReadByte() << 24));
                        break;
                    case 2:
                        _insts.AddOp(OperatorName.hintmask2,
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16)
                            );
                        break;
                    case 3:
                        _insts.AddOp(OperatorName.hintmask3,
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16) |
                            (reader.ReadByte() << 8)
                            );
                        break;
                    case 4:
                        _insts.AddOp(OperatorName.hintmask4,
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16) |
                            (reader.ReadByte() << 8) |
                            (reader.ReadByte())
                            );
                        break;
                }
            }
        }
        /// <summary>
        /// cntrmask
        /// </summary>
        /// <param name="reader"></param>
        void AddCounterMaskToList(ref SimpleBinaryReader reader)
        {
            if (_hintStemCount == 0)
            {
                if (!_foundSomeStem)
                {
                    //????
                    _hintStemCount = (_current_integer_count / 2);
                    _foundSomeStem = true;//?
                }
                else
                {
                    throw new OpenFontNotSupportedException();
                }
            }
            else
            {
                _hintStemCount += (_current_integer_count / 2);
            }
            //---------------------- 
            //this is my hintmask extension, => to fit with our Evaluation stack
            int properNumberOfMaskBytes = (_hintStemCount + 7) / 8;
            if (reader.Position + properNumberOfMaskBytes >= reader.BufferLength)
            {
                throw new OpenFontNotSupportedException();
            }

            if (properNumberOfMaskBytes > 4)
            {
                int remaining = properNumberOfMaskBytes;

                for (; remaining > 3;)
                {
                    _insts.AddInt((
                       (reader.ReadByte() << 24) |
                       (reader.ReadByte() << 16) |
                       (reader.ReadByte() << 8) |
                       (reader.ReadByte())
                       ));
                    remaining -= 4; //*** 
                }
                switch (remaining)
                {
                    case 0:
                        //do nothing
                        break;
                    case 1:
                        _insts.AddInt(reader.ReadByte() << 24);
                        break;
                    case 2:
                        _insts.AddInt(
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16));

                        break;
                    case 3:
                        _insts.AddInt(
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16) |
                            (reader.ReadByte() << 8));
                        break;
                    default: throw new OpenFontNotSupportedException();//should not occur !
                }

                _insts.AddOp(OperatorName.cntrmask_bits, properNumberOfMaskBytes);
            }
            else
            {
                //last remaining <4 bytes 
                switch (properNumberOfMaskBytes)
                {
                    case 0:
                    default: throw new OpenFontNotSupportedException();//should not occur !
                    case 1:
                        _insts.AddOp(OperatorName.cntrmask1, (reader.ReadByte() << 24));
                        break;
                    case 2:
                        _insts.AddOp(OperatorName.cntrmask2,
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16)
                            );
                        break;
                    case 3:
                        _insts.AddOp(OperatorName.cntrmask3,
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16) |
                            (reader.ReadByte() << 8)
                            );
                        break;
                    case 4:
                        _insts.AddOp(OperatorName.cntrmask4,
                            (reader.ReadByte() << 24) |
                            (reader.ReadByte() << 16) |
                            (reader.ReadByte() << 8) |
                            (reader.ReadByte())
                            );
                        break;
                }
            }
        }

        static int ReadIntegerNumber(ref SimpleBinaryReader _reader, byte b0)
        {
            if (b0 >= 32 && b0 <= 246)
            {
                return b0 - 139;
            }
            else if (b0 <= 250)  // && b0 >= 247 , *** if-else sequence is important! ***
            {
                byte b1 = _reader.ReadByte();
                return (b0 - 247) * 256 + b1 + 108;
            }
            else if (b0 <= 254)  //&&  b0 >= 251 ,*** if-else sequence is important! ***
            {
                byte b1 = _reader.ReadByte();
                return -(b0 - 251) * 256 - b1 - 108;
            }
            else
            {
                throw new OpenFontNotSupportedException();
            }
        }
    }


}