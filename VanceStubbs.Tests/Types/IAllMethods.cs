namespace VanceStubbs.Tests.Types
{
    using System;
    using System.Collections.Generic;
    using System.IO;

    public interface IAllMethods
    {
        void Void();

        byte Byte();

        char Char();

        bool Bool();

        ushort Ushort();

        short Short();

        uint Uint();

        int Int();

        ulong Ulong();

        long Long();

        float Float();

        double Double();

        decimal Decimal();

        string String();

        object Object();

        DateTime CustomStruct();

        Stream CustomClass();

        List<int> Generic();
    }
}
