//
// C# (.Net Framework)
// dkxce.DllExportList
// v 0.1, 17.07.2023
// dkxce (https://github.com/dkxce/DllExportList)
// en,ru,1251,utf-8
//

using System;
using System.IO;
using System.Text;

namespace dkxce
{
    public struct DllExportList
    {
        private static readonly Encoding DefaultEncoding = Encoding.ASCII;

        public struct ExportFunction
        {
            /// <summary>
            ///     Pointer To Function Name Null-Terminated String
            /// </summary>
            private uint NamePtr;

            /// <summary>
            ///     Name of Exported Function
            /// </summary>
            public string Name;

            /// <summary>
            ///     Entry Point of Exported Function
            /// </summary>
            public string EntryPoint;

            /// <summary>
            ///     Ordinal of Exported Function
            /// </summary>
            public uint Ordinal;

            /// <summary>
            ///     Pointer of Exported Function
            /// </summary>
            public uint Address;

            public override string ToString()
            {
                return $"{Ordinal:D2} {Name}, hex: {EntryPoint}, dec: {Address}";
            }

            internal static ExportFunction Create(uint NamePtr)
            {
                ExportFunction res = new ExportFunction();
                res.NamePtr = NamePtr;
                return res;
            }
        }

        /// <summary>
        ///     Pointer To Module Name Null-Terminated String
        /// </summary>
        private uint ModuleNamePtr;

        /// <summary>
        ///     DLL Module Name
        /// </summary>
        public string ModuleName;

        /// <summary>
        ///     DLL Ordinal Base
        /// </summary>
        public uint OrdinalBase;

        /// <summary>
        ///     DLL Exported Function Count
        /// </summary>
        public uint FunctionsCount;

        /// <summary>
        ///     DLL Exported Function Names Count
        /// </summary>
        public uint NamesCount;

        /// <summary>
        ///     Is x86 (32-bit)
        /// </summary>
        public bool x86;

        /// <summary>
        ///     Is x64 (64-bit)
        /// </summary>
        public bool x64;

        public ExportFunction[] Functions;

        public static DllExportList GetDllExportFunctions(string fileName)
        {
            const int DEF_SHORT_SIZE = 0x0002;
            const int DEF_INT_SIZE = 0x0004;
            const int PESignatureOffset = 0x003C;
            const int PESignatureSize = 0x0004;
            const int COFFHeaderSize = 0x0014;
            const int SectionHeaderSize = 0x0028;
            const int ARCHITECTURE_I386 = 0x014C;
            const int ARCHITECTURE_AMD64 = 0x8664;
            const int IMAGE_FILE_IS_DLL = 0x2000;
            const int IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x0020B;
            const string DEF_DLL_START = "MZ";
            const string DEF_PES_START = "PE\0\0";

            using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
            {
                if (fs.Length == 0) throw new IOException("DLL File is Empty");

                byte[] RAWDATA = new byte[DEF_SHORT_SIZE]; fs.Read(RAWDATA, 0, RAWDATA.Length);
                if (DefaultEncoding.GetString(RAWDATA) != DEF_DLL_START) throw new IOException("DLL File Invalid, no MS-DOS stub");

                fs.Position = PESignatureOffset;
                RAWDATA = new byte[DEF_INT_SIZE]; fs.Read(RAWDATA, 0, RAWDATA.Length);
                fs.Position = BitConverter.ToInt32(RAWDATA, 0);

                RAWDATA = new byte[PESignatureSize]; fs.Read(RAWDATA, 0, RAWDATA.Length);
                if (DefaultEncoding.GetString(RAWDATA) != DEF_PES_START) throw new IOException("PE Signature not found");

                RAWDATA = new byte[COFFHeaderSize]; fs.Read(RAWDATA, 0, RAWDATA.Length);
                ushort Machine = BitConverter.ToUInt16(RAWDATA, 0);
                ushort NumberOfSections = BitConverter.ToUInt16(RAWDATA, 2);
                ushort SizeOfOptionalHeader = BitConverter.ToUInt16(RAWDATA, 16);
                ushort Characteristics = BitConverter.ToUInt16(RAWDATA, 18);
                if ((Machine != ARCHITECTURE_I386) && (Machine != ARCHITECTURE_AMD64)) throw new Exception($"CPU Type {Machine} Not Supported");
                if ((Characteristics & IMAGE_FILE_IS_DLL) == 0) throw new Exception("Invalid DLL file");

                RAWDATA = new byte[SizeOfOptionalHeader]; fs.Read(RAWDATA, 0, RAWDATA.Length);
                ushort btns = BitConverter.ToUInt16(RAWDATA, 0);
                int offset = (btns == IMAGE_NT_OPTIONAL_HDR64_MAGIC) ? 16 : 0;
                uint SizeOfImage = BitConverter.ToUInt32(RAWDATA, 56);

                offset += 92;
                uint NumberOfRvaAndSizes = BitConverter.ToUInt32(RAWDATA, offset + 0);
                uint ExportAddr = BitConverter.ToUInt32(RAWDATA, offset + 4);
                uint ExportSize = BitConverter.ToUInt32(RAWDATA, offset + 8);
                if (NumberOfRvaAndSizes < 1 /* the number of data directory entries */ || ExportAddr < 1 /* the address of the export table(RVA) */ || ExportSize < 1 /* the size of the export table */)
                    throw new Exception("Couldn't find an Export table");

                uint SectionsLength = (uint)(SectionHeaderSize * NumberOfSections);
                RAWDATA = new byte[SectionsLength]; fs.Read(RAWDATA, 0, RAWDATA.Length);
                byte[] ImageData = new byte[SizeOfImage];

                int off = 0;
                for (int i = 0; i < NumberOfSections; i++)
                {
                    int VirtualAddress = BitConverter.ToInt32(RAWDATA, off + 12);
                    int SizeOfRawData = BitConverter.ToInt32(RAWDATA, off + 16);
                    int PointerToRawData = BitConverter.ToInt32(RAWDATA, off + 20);
                    fs.Position = PointerToRawData;
                    fs.Read(ImageData, VirtualAddress, SizeOfRawData);
                    off += SectionHeaderSize;
                };

                Func<byte[], int, string> BytesToStr = (byte[] bytes, int ofset) =>
                {
                    string res = "";
                    int i = ofset;
                    while (i < bytes.Length)
                    {
                        byte b = bytes[i++];
                        if (b == 0) return res;
                        res += (char)b;
                    };
                    return res;
                };

                uint EndOfSection = ExportAddr + ExportSize;

                DllExportList dlle = new DllExportList();
                dlle.x86 = btns != IMAGE_NT_OPTIONAL_HDR64_MAGIC;
                dlle.x64 = btns == IMAGE_NT_OPTIONAL_HDR64_MAGIC;
                dlle.ModuleNamePtr = BitConverter.ToUInt32(ImageData, (int)ExportAddr + 12);
                dlle.OrdinalBase = BitConverter.ToUInt32(ImageData, (int)ExportAddr + 16);
                dlle.FunctionsCount = BitConverter.ToUInt32(ImageData, (int)ExportAddr + 20);
                dlle.NamesCount = BitConverter.ToUInt32(ImageData, (int)ExportAddr + 24);
                dlle.Functions = new DllExportList.ExportFunction[dlle.FunctionsCount];

                uint FuncTblPtr = BitConverter.ToUInt32(ImageData, (int)ExportAddr + 28);
                uint NameTblPtr = BitConverter.ToUInt32(ImageData, (int)ExportAddr + 32);
                uint OrdTblPtr = BitConverter.ToUInt32(ImageData, (int)ExportAddr + 36);

                dlle.ModuleName = BytesToStr(ImageData, (int)dlle.ModuleNamePtr);

                for (int i = 0; i < dlle.FunctionsCount; i++)
                {
                    uint NamePtr = BitConverter.ToUInt32(ImageData, (int)NameTblPtr);
                    dlle.Functions[i] = DllExportList.ExportFunction.Create(NamePtr);
                    dlle.Functions[i].Ordinal = BitConverter.ToUInt16(ImageData, (int)OrdTblPtr);
                    dlle.Functions[i].Address = BitConverter.ToUInt32(ImageData, (int)FuncTblPtr + ((int)dlle.Functions[i].Ordinal * 4));
                    dlle.Functions[i].Ordinal += dlle.OrdinalBase;
                    dlle.Functions[i].EntryPoint = (dlle.Functions[i].Address > ExportAddr) && (dlle.Functions[i].Address < EndOfSection) ? BytesToStr(ImageData, (int)dlle.Functions[i].Address) : "0x" + dlle.Functions[i].Address.ToString("X8");
                    dlle.Functions[i].Name = BytesToStr(ImageData, (int)NamePtr);
                    NameTblPtr += 4;
                    OrdTblPtr += 2;
                };

                return dlle;
            };
        }

        public override string ToString()
        {
            return $"{ModuleName} ({FunctionsCount} functions with {NamesCount} names from {OrdinalBase} ordinal base";
        }
    }
}
