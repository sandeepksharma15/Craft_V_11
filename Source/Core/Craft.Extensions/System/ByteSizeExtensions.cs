#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System;
#pragma warning restore IDE0130 // Namespace does not match folder structure

public static class ByteSizeExtensions
{
    extension(long bytes)
    {
        public double ToKilobytes()
            => bytes / 1_000d;

        public double ToKibibytes()
            => bytes / 1024d;

        public double ToMegabytes()
            => bytes / 1_000_000d;

        public double ToMebibytes()
            => bytes / (1024d * 1024d);

        public double ToGigabytes()
            => bytes / 1_000_000_000d;

        public double ToGibibytes()
            => bytes / (1024d * 1024d * 1024d);
    }
}
