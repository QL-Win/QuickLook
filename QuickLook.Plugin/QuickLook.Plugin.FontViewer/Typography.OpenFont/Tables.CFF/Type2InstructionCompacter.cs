//MIT, 2020-present, WinterDev  

using System;
using System.Collections.Generic;


namespace Typography.OpenFont.CFF
{

    class Type2InstructionCompacter
    {
        //This is our extension
        //-----------------------
#if DEBUG
        public static bool s_dbugBreakMe;
#endif
        List<Type2Instruction> _step1List;
        List<Type2Instruction> _step2List;

        void CompactStep1OnlyLoadInt(List<Type2Instruction> insts)
        {
            int j = insts.Count;
            CompactRange _latestCompactRange = CompactRange.None;
            int startCollectAt = -1;
            int collecting_count = 0;
            void FlushWaitingNumbers()
            {
                //Nested method
                //flush waiting integer
                if (_latestCompactRange == CompactRange.Short)
                {
                    switch (collecting_count)
                    {
                        default: throw new OpenFontNotSupportedException();
                        case 0: break; //nothing
                        case 2:
                            _step1List.Add(new Type2Instruction(OperatorName.LoadShort2,
                                      (((ushort)insts[startCollectAt].Value) << 16) |
                                      (((ushort)insts[startCollectAt + 1].Value))
                                      ));
                            startCollectAt += 2;
                            collecting_count -= 2;
                            break;
                        case 1:
                            _step1List.Add(insts[startCollectAt]);
                            startCollectAt += 1;
                            collecting_count -= 1;
                            break;

                    }
                }
                else
                {
                    switch (collecting_count)
                    {
                        default: throw new OpenFontNotSupportedException();
                        case 0: break;//nothing
                        case 4:
                            {
                                _step1List.Add(new Type2Instruction(OperatorName.LoadSbyte4,
                                   (((byte)insts[startCollectAt].Value) << 24) |
                                   (((byte)insts[startCollectAt + 1].Value) << 16) |
                                   (((byte)insts[startCollectAt + 2].Value) << 8) |
                                   (((byte)insts[startCollectAt + 3].Value) << 0)
                                   ));
                                startCollectAt += 4;
                                collecting_count -= 4;
                            }
                            break;
                        case 3:
                            _step1List.Add(new Type2Instruction(OperatorName.LoadSbyte3,
                                (((byte)insts[startCollectAt].Value) << 24) |
                                (((byte)insts[startCollectAt + 1].Value) << 16) |
                                (((byte)insts[startCollectAt + 2].Value) << 8)
                                ));
                            startCollectAt += 3;
                            collecting_count -= 3;
                            break;
                        case 2:
                            _step1List.Add(new Type2Instruction(OperatorName.LoadShort2,
                              (((ushort)insts[startCollectAt].Value) << 16) |
                              ((ushort)insts[startCollectAt + 1].Value)
                              ));
                            startCollectAt += 2;
                            collecting_count -= 2;
                            break;
                        case 1:
                            _step1List.Add(insts[startCollectAt]);
                            startCollectAt += 1;
                            collecting_count -= 1;
                            break;

                    }
                }

                startCollectAt = -1;
                collecting_count = 0;
            }

            for (int i = 0; i < j; ++i)
            {
                Type2Instruction inst = insts[i];
                if (inst.IsLoadInt)
                {
                    //check waiting data in queue
                    //get compact range
                    CompactRange c1 = GetCompactRange(inst.Value);
                    switch (c1)
                    {
                        default: throw new OpenFontNotSupportedException();
                        case CompactRange.None:
                            {
                                if (collecting_count > 0)
                                {
                                    FlushWaitingNumbers();
                                }
                                _step1List.Add(inst);
                                _latestCompactRange = CompactRange.None;
                            }
                            break;
                        case CompactRange.SByte:
                            {
                                if (_latestCompactRange == CompactRange.Short)
                                {
                                    FlushWaitingNumbers();
                                    _latestCompactRange = CompactRange.SByte;
                                }

                                switch (collecting_count)
                                {
                                    default: throw new OpenFontNotSupportedException();
                                    case 0:
                                        startCollectAt = i;
                                        _latestCompactRange = CompactRange.SByte;
                                        break;
                                    case 1:
                                        break;
                                    case 2:
                                        break;
                                    case 3:
                                        //we already have 3 bytes
                                        //so this is 4th byte
                                        collecting_count++;
                                        FlushWaitingNumbers();
                                        continue;
                                }
                                collecting_count++;
                            }
                            break;
                        case CompactRange.Short:
                            {
                                if (_latestCompactRange == CompactRange.SByte)
                                {
                                    FlushWaitingNumbers();
                                    _latestCompactRange = CompactRange.Short;
                                }

                                switch (collecting_count)
                                {
                                    default: throw new OpenFontNotSupportedException();
                                    case 0:
                                        startCollectAt = i;
                                        _latestCompactRange = CompactRange.Short;
                                        break;
                                    case 1:
                                        //we already have 1 so this is 2nd 
                                        collecting_count++;
                                        FlushWaitingNumbers();
                                        continue;
                                }

                                collecting_count++;
                            }
                            break;
                    }
                }
                else
                {
                    //other cmds
                    //flush waiting cmd
                    if (collecting_count > 0)
                    {
                        FlushWaitingNumbers();
                    }

                    _step1List.Add(inst);
                    _latestCompactRange = CompactRange.None;
                }
            }
        }


        static byte IsLoadIntOrMergeableLoadIntExtension(OperatorName opName)
        {
            switch (opName)
            {
                //case OperatorName.LoadSbyte3://except LoadSbyte3 ***                
                case OperatorName.LoadInt: //merge-able
                    return 1;
                case OperatorName.LoadShort2://merge-able
                    return 2;
                case OperatorName.LoadSbyte4://merge-able
                    return 3;
            }
            return 0;
        }
        void CompactStep2MergeLoadIntWithNextCommand()
        {
            //a second pass
            //check if we can merge some load int( LoadInt, LoadSByte4, LoadShort2) except LoadSByte3 
            //to next instruction command or not
            int j = _step1List.Count;
            for (int i = 0; i < j; ++i)
            {
                Type2Instruction i0 = _step1List[i];

                if (i + 1 < j)
                {
                    //has next cmd           
                    byte merge_flags = IsLoadIntOrMergeableLoadIntExtension((OperatorName)i0.Op);
                    if (merge_flags > 0)
                    {
                        Type2Instruction i1 = _step1List[i + 1];
                        //check i1 has empty space for i0 or not
                        bool canbe_merged = false;
                        switch ((OperatorName)i1.Op)
                        {
                            case OperatorName.LoadInt:
                            case OperatorName.LoadShort2:
                            case OperatorName.LoadSbyte4:
                            case OperatorName.LoadSbyte3:
                            case OperatorName.LoadFloat:

                            case OperatorName.hintmask1:
                            case OperatorName.hintmask2:
                            case OperatorName.hintmask3:
                            case OperatorName.hintmask4:
                            case OperatorName.hintmask_bits:
                            case OperatorName.cntrmask1:
                            case OperatorName.cntrmask2:
                            case OperatorName.cntrmask3:
                            case OperatorName.cntrmask4:
                            case OperatorName.cntrmask_bits:
                                break;
                            default:
                                canbe_merged = true;
                                break;
                        }
                        if (canbe_merged)
                        {

#if DEBUG
                            if (merge_flags > 3) { throw new OpenFontNotSupportedException(); }
#endif

                            _step2List.Add(new Type2Instruction((byte)((merge_flags << 6) | i1.Op), i0.Value));
                            i += 1;
                        }
                        else
                        {
                            _step2List.Add(i0);
                        }
                    }
                    else
                    {
                        //this is the last one
                        _step2List.Add(i0);
                    }

                }
                else
                {
                    //this is the last one
                    _step2List.Add(i0);
                }
            }
        }
        public Type2Instruction[] Compact(List<Type2Instruction> insts)
        {
            //for simpicity
            //we have 2 passes
            //1. compact consecutive numbers
            //2. compact other cmd

            //reset
            if (_step1List == null)
            {
                _step1List = new List<Type2Instruction>();
            }
            if (_step2List == null)
            {
                _step2List = new List<Type2Instruction>();
            }
            _step1List.Clear();
            _step2List.Clear();
            //
            CompactStep1OnlyLoadInt(insts);
            CompactStep2MergeLoadIntWithNextCommand();
#if DEBUG

            //you can check/compare the compact form and the original form
            dbugReExpandAndCompare_ForStep1(_step1List, insts);
            dbugReExpandAndCompare_ForStep2(_step2List, insts);
#endif
            return _step2List.ToArray();
            //return _step1List.ToArray();

        }

#if DEBUG
        void dbugReExpandAndCompare_ForStep1(List<Type2Instruction> step1, List<Type2Instruction> org)
        {
            List<Type2Instruction> expand1 = new List<Type2Instruction>(org.Count);
            {
                int j = step1.Count;
                for (int i = 0; i < j; ++i)
                {
                    Type2Instruction inst = step1[i];
                    switch ((OperatorName)inst.Op)
                    {
                        case OperatorName.LoadSbyte4:
                            expand1.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 24)));
                            expand1.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 16)));
                            expand1.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 8)));
                            expand1.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value)));
                            break;
                        case OperatorName.LoadSbyte3:
                            expand1.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 24)));
                            expand1.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 16)));
                            expand1.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 8)));
                            break;
                        case OperatorName.LoadShort2:
                            expand1.Add(new Type2Instruction(OperatorName.LoadInt, (short)(inst.Value >> 16)));
                            expand1.Add(new Type2Instruction(OperatorName.LoadInt, (short)(inst.Value)));
                            break;
                        default:
                            expand1.Add(inst);
                            break;
                    }
                }
            }
            //--------------------------------------------
            if (expand1.Count != org.Count)
            {
                //ERR=> then find first diff
                int min = Math.Min(expand1.Count, org.Count);
                for (int i = 0; i < min; ++i)
                {
                    Type2Instruction inst_exp = expand1[i];
                    Type2Instruction inst_org = org[i];
                    if (inst_exp.Op != inst_org.Op ||
                       inst_exp.Value != inst_org.Value)
                    {
                        throw new OpenFontNotSupportedException();
                    }
                }
            }
            else
            {
                //compare command-by-command
                int j = step1.Count;
                for (int i = 0; i < j; ++i)
                {
                    Type2Instruction inst_exp = expand1[i];
                    Type2Instruction inst_org = org[i];
                    if (inst_exp.Op != inst_org.Op ||
                       inst_exp.Value != inst_org.Value)
                    {
                        throw new OpenFontNotSupportedException();
                    }
                }
            }

        }
        void dbugReExpandAndCompare_ForStep2(List<Type2Instruction> step2, List<Type2Instruction> org)
        {
            List<Type2Instruction> expand2 = new List<Type2Instruction>(org.Count);
            {
                int j = step2.Count;
                for (int i = 0; i < j; ++i)
                {

                    Type2Instruction inst = step2[i];

                    //we use upper 2 bits to indicate that this is merged cmd or not
                    byte merge_flags = (byte)(inst.Op >> 6);
                    //lower 6 bits is actual cmd
                    OperatorName onlyOpName = (OperatorName)(inst.Op & 0b111111);
                    switch (onlyOpName)
                    {
                        case OperatorName.LoadSbyte4:
                            expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 24)));
                            expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 16)));
                            expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 8)));
                            expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value)));
                            break;
                        case OperatorName.LoadSbyte3:
                            expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 24)));
                            expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 16)));
                            expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 8)));
                            break;
                        case OperatorName.LoadShort2:
                            expand2.Add(new Type2Instruction(OperatorName.LoadInt, (short)(inst.Value >> 16)));
                            expand2.Add(new Type2Instruction(OperatorName.LoadInt, (short)(inst.Value)));
                            break;
                        default:
                            {
                                switch (merge_flags)
                                {
                                    case 0:
                                        expand2.Add(inst);
                                        break;
                                    case 1:
                                        expand2.Add(new Type2Instruction(OperatorName.LoadInt, inst.Value));
                                        expand2.Add(new Type2Instruction(onlyOpName, 0));
                                        break;
                                    case 2:
                                        expand2.Add(new Type2Instruction(OperatorName.LoadInt, (short)(inst.Value >> 16)));
                                        expand2.Add(new Type2Instruction(OperatorName.LoadInt, (short)(inst.Value)));
                                        expand2.Add(new Type2Instruction(onlyOpName, 0));
                                        break;
                                    case 3:
                                        expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 24)));
                                        expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 16)));
                                        expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value >> 8)));
                                        expand2.Add(new Type2Instruction(OperatorName.LoadInt, (sbyte)(inst.Value)));
                                        expand2.Add(new Type2Instruction(onlyOpName, 0));
                                        break;
                                }
                            }
                            break;
                    }
                }
            }
            //--------------------------------------------
            if (expand2.Count != org.Count)
            {
                throw new OpenFontNotSupportedException();
            }
            else
            {
                //compare command-by-command
                int j = step2.Count;
                for (int i = 0; i < j; ++i)
                {
                    Type2Instruction inst_exp = expand2[i];
                    Type2Instruction inst_org = org[i];
                    if (inst_exp.Op != inst_org.Op ||
                       inst_exp.Value != inst_org.Value)
                    {
                        throw new OpenFontNotSupportedException();
                    }
                }
            }

        }
#endif
        enum CompactRange
        {
            None,
            //
            SByte,
            Short,
        }

        static CompactRange GetCompactRange(int value)
        {
            if (value > sbyte.MinValue && value < sbyte.MaxValue)
            {
                return CompactRange.SByte;
            }
            else if (value > short.MinValue && value < short.MaxValue)
            {
                return CompactRange.Short;
            }
            else
            {
                return CompactRange.None;
            }
        }
    }

}