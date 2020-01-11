using System;
using System.Collections.Generic;
using System.Linq;

namespace CTC_Fixer.FileDefinition
{
    public static class Constants
    {
        public static int TypeACountLocation = 16;
        public static int TypeBCountLocation = 20;
        public static int HeaderByteSize = 80;
        public static int TypeAByteSize = 80;
        public static int TypeBByteSize = 96;
        public static int TypeBIcebornByteSize = 112;

        public static byte FromHexToByte(this string hex)
        {
            return Convert.ToByte(hex, 16);
        }

        public static byte[] FromHexToByte(this string[] hexes)
        {
            return hexes.Select(FromHexToByte).ToArray();
        }

        public static byte[] FromHexStringToByte(this string hex)
        {
            return hex.Split(' ').Select(FromHexToByte).ToArray();
        }
    }

    public static class Helper
    {
        public static byte[] GetNextSection(this byte[] bytes, ref int offset, int length)
        {
            var newBytes = bytes.Skip(offset).Take(length).ToArray();
            offset += length;

            return newBytes;
        }

        public static bool CompareBytes(byte[] first, byte[] second)
        {
            // If not same length, done
            if (first.Length != second.Length)
            {
                return false;
            }

            // If they are the same object, done
            if (ReferenceEquals(first, second))
            {
                return true;
            }

            // Loop all values and compare
            for (var i = 0; i < first.Length; i++)
            {
                if (first[i] != second[i])
                {
                    return false;
                }
            }

            // If we got here, equal
            return true;
        }
    }

    public class CTC
    {
        //These help determine how long the bytes should be and if it is valid or not

        private bool IsIceborneCTC = false;
        public CTCHeader Header { get; set; }
        public List<CTCTypeA> TypeAList { get; set; } = new List<CTCTypeA>();
        public CTC(byte[] bytes)
        {
            Header = new CTCHeader(bytes.Take(Constants.HeaderByteSize).ToArray());

            int typeACount = bytes[Constants.TypeACountLocation];
            int typeBCount = bytes[Constants.TypeBCountLocation];

            var predictedByteSize = Constants.HeaderByteSize + (typeACount * Constants.TypeAByteSize) + (typeBCount * Constants.TypeBByteSize);
            var predictedIceborneByteSize = Constants.HeaderByteSize + (typeACount * Constants.TypeAByteSize) + (typeBCount * Constants.TypeBIcebornByteSize);

            if (predictedIceborneByteSize == bytes.Length)
            {
                IsIceborneCTC = true;
            }

            if (predictedByteSize != bytes.Length && !IsIceborneCTC)
            {
                throw new Exception("File size is not valid, as they do not match the byte sizes allocated.");
            }

            var offset = Constants.HeaderByteSize;

            for (var typeA = 0; typeA < typeACount; typeA++)
            {
                var typeASection = bytes.GetNextSection(ref offset, Constants.TypeAByteSize);

                TypeAList.Add(new CTCTypeA(typeASection));
            }

            foreach (var typeA in TypeAList)
            {
                for (var typeB = 0; typeB < typeA.TypeBSectionCount; typeB++)
                {
                    //Skip header, type A section, previous b sections and then apply this typeB count
                    var typeBSection = bytes.GetNextSection(ref offset, IsIceborneCTC ? Constants.TypeBIcebornByteSize : Constants.TypeBByteSize);

                    typeA.TypeBList.Add(new CTCTypeB(typeBSection));
                }
            }
        }

        public byte[] GenerateIceborneFromOriginalBytes()
        {
            var tmpBytes = new byte[0];

            tmpBytes = tmpBytes.Concat(Header.GetBytes()).ToArray();

            foreach (var array in TypeAList.Select(z => z.GetBytes()))
            {
                tmpBytes = tmpBytes.Concat(array).ToArray();
            }

            foreach (var array in TypeAList)
            {
                foreach (var arrayB in array.TypeBList)
                {
                    tmpBytes = tmpBytes.Concat(arrayB.GetIceborneBytes()).ToArray();
                }
            }

            return tmpBytes;
        }

        public byte[] GenerateFromOriginalBytes()
        {
            var tmpBytes = new byte[0];

            tmpBytes = tmpBytes.Concat(Header.GetBytes()).ToArray();

            foreach (var array in TypeAList.Select(z => z.GetBytes()))
            {
                tmpBytes = tmpBytes.Concat(array).ToArray();
            }

            foreach (var array in TypeAList)
            {
                foreach (var arrayB in array.TypeBList)
                {
                    tmpBytes = tmpBytes.Concat(arrayB.GetBytes()).ToArray();
                }
            }

            return tmpBytes;
        }

        public byte[] GenerateFromOriginalBytes(byte[] bytes)
        {
            var tmpBytes = new byte[0];
            var offset = 0;

            tmpBytes = tmpBytes.Concat(Header.GetBytes()).ToArray();

            foreach (var array in TypeAList.Select(z => z.GetBytes()))
            {
                tmpBytes = tmpBytes.Concat(array).ToArray();

                offset = tmpBytes.Length;

                var same = Helper.CompareBytes(bytes.Take(offset).ToArray(), tmpBytes);
            }

            foreach (var array in TypeAList)
            {
                foreach (var arrayB in array.TypeBList)
                {
                    tmpBytes = tmpBytes.Concat(arrayB.GetBytes()).ToArray();

                    offset = tmpBytes.Length;

                    var same = Helper.CompareBytes(bytes.Take(offset).ToArray(), tmpBytes);
                }
            }

            return tmpBytes;
        }

        public byte[] GenerateFromSections()
        {
            return null;
        }
    }

    public class CTCHeader
    {
        private byte[] _originalBytes = new byte[Constants.HeaderByteSize];

        public CTCHeader(byte[] bytes)
        {
            if (bytes.Length != Constants.HeaderByteSize)
            {
                throw new Exception("Bytes do not match header length.");
            }

            //Keep a copy of the original bytes
            bytes.CopyTo(_originalBytes, 0);

            //Check byte 5 to make sure it is to from 1B to 1C
            if (_originalBytes[4] == "1B".FromHexToByte())
            {
                _originalBytes[4] = "1C".FromHexToByte();
            }
        }

        public byte[] GetBytes()
        {
            return _originalBytes;
        }
    }

    public class CTCTypeA
    {
        private byte[] _originalBytes = new byte[Constants.TypeAByteSize];

        public List<CTCTypeB> TypeBList { get; set; } = new List<CTCTypeB>();
        public int TypeBSectionCount { get; }
        public int CollisionDetection { get; }
        public int CollisionWeight { get; }

        public CTCTypeA(byte[] bytes)
        {
            //Extract and assign each section as it is identified
            if (bytes.Length != Constants.TypeAByteSize)
            {
                throw new Exception("Bytes do not match Type A length");
            }

            //Keep a copy of the original bytes
            bytes.CopyTo(_originalBytes, 0);

            //Set up variables
            //First byte contains how many b sections there are associated
            TypeBSectionCount = bytes[0];
        }

        public byte[] GetBytes()
        {
            return _originalBytes;
        }
    }

    public class CTCTypeB
    {
        private byte[] _originalBytes = new byte[Constants.TypeBByteSize];

        public CTCTypeB(byte[] bytes)
        {
            //Extract and assign each section as it is identified
            if (bytes.Length != Constants.TypeBByteSize && bytes.Length != Constants.TypeBIcebornByteSize)
            {
                throw new Exception("Bytes do not match Type B length");
            }

            //Keep a copy of the original bytes
            _originalBytes = bytes;
        }

        public byte[] GetBytes()
        {
            return _originalBytes;
        }

        public byte[] GetIceborneBytes()
        {
            var newBytes = new byte[0];

            newBytes = newBytes.Concat(_originalBytes).ToArray();

            if (newBytes.Length != Constants.TypeBIcebornByteSize)
            {
                var iceBorneAddedSection = "00 00 80 3F CD CD CD CD CD CD CD CD CD CD CD CD";

                var iceBorneBytes = iceBorneAddedSection.FromHexStringToByte();

                newBytes = newBytes.Concat(iceBorneBytes).ToArray();
            }

            return newBytes;
        }
    }
}