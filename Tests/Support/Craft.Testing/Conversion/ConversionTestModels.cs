namespace Craft.Testing.Conversion;

public static class ConversionTestModels
{
    #region Models

    [global::System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2208:Instantiate argument exceptions correctly", Justification = "<Pending>")]
    public sealed class ArgumentThrowingConvertible : IConvertible
    {
        #region Public Methods

        public TypeCode GetTypeCode() => TypeCode.Object;

        public bool ToBoolean(IFormatProvider? provider) => throw new NotSupportedException();

        public byte ToByte(IFormatProvider? provider) => throw new NotSupportedException();

        public char ToChar(IFormatProvider? provider) => throw new NotSupportedException();

        public DateTime ToDateTime(IFormatProvider? provider) => throw new NotSupportedException();

        public decimal ToDecimal(IFormatProvider? provider) => throw new NotSupportedException();

        public double ToDouble(IFormatProvider? provider) => throw new NotSupportedException();

        public short ToInt16(IFormatProvider? provider) => throw new NotSupportedException();

        public int ToInt32(IFormatProvider? provider) => throw new ArgumentException();

        public long ToInt64(IFormatProvider? provider) => throw new NotSupportedException();

        public sbyte ToSByte(IFormatProvider? provider) => throw new NotSupportedException();

        public float ToSingle(IFormatProvider? provider) => throw new NotSupportedException();

        public string ToString(IFormatProvider? provider) => throw new NotSupportedException();

        public object ToType(Type conversionType, IFormatProvider? provider) => throw new ArgumentException();

        public ushort ToUInt16(IFormatProvider? provider) => throw new NotSupportedException();

        public uint ToUInt32(IFormatProvider? provider) => throw new NotSupportedException();

        public ulong ToUInt64(IFormatProvider? provider) => throw new NotSupportedException();

        #endregion Public Methods
    }

    public sealed class InvalidCastConvertible : IConvertible
    {
        #region Public Methods

        public TypeCode GetTypeCode() => TypeCode.Object;

        public bool ToBoolean(IFormatProvider? provider) => throw new NotSupportedException();

        public byte ToByte(IFormatProvider? provider) => throw new NotSupportedException();

        public char ToChar(IFormatProvider? provider) => throw new NotSupportedException();

        public DateTime ToDateTime(IFormatProvider? provider) => throw new NotSupportedException();

        public decimal ToDecimal(IFormatProvider? provider) => throw new NotSupportedException();

        public double ToDouble(IFormatProvider? provider) => throw new NotSupportedException();

        public short ToInt16(IFormatProvider? provider) => throw new NotSupportedException();

        public int ToInt32(IFormatProvider? provider) => throw new InvalidCastException();

        public long ToInt64(IFormatProvider? provider) => throw new NotSupportedException();

        public sbyte ToSByte(IFormatProvider? provider) => throw new NotSupportedException();

        public float ToSingle(IFormatProvider? provider) => throw new NotSupportedException();

        public string ToString(IFormatProvider? provider) => throw new NotSupportedException();

        public object ToType(Type conversionType, IFormatProvider? provider) => throw new InvalidCastException();

        public ushort ToUInt16(IFormatProvider? provider) => throw new NotSupportedException();

        public uint ToUInt32(IFormatProvider? provider) => throw new NotSupportedException();

        public ulong ToUInt64(IFormatProvider? provider) => throw new NotSupportedException();

        #endregion Public Methods
    }

    #endregion Models
}
