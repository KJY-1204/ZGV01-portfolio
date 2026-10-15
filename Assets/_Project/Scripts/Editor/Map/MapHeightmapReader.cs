// 16비트 그레이스케일(또는 RGBA16) PNG 하이트맵에서 구간을 잘라 0~1 높이 배열로 읽는 에디터 도구다. 텍스처 임포터는 16비트를 8비트로 줄여 계단이 생기므로 PNG를 직접 해석한다.
using System;
using System.IO;
using System.IO.Compression;
using UnityEngine;

public static class MapHeightmapReader
{
    // path의 PNG에서 (cropX, cropY)부터 cropSize 정사각 구간을 outRes x outRes로 쌍선형 표본추출해 0~1로 돌려준다. 결과 [z, x]에서 z는 PNG 행이다.
    public static float[,] Read(string path, int cropX, int cropY, int cropSize, int outRes)
    {
        byte[] file = File.ReadAllBytes(path);
        if (file.Length < 33 || file[0] != 0x89 || file[1] != 'P') throw new InvalidDataException("PNG가 아니다: " + path);

        int width = 0, height = 0, bitDepth = 0, colorType = 0;
        MemoryStream compressed = new MemoryStream();
        int pos = 8;
        while (pos < file.Length)
        {
            int length = ReadInt(file, pos);
            string type = System.Text.Encoding.ASCII.GetString(file, pos + 4, 4);
            if (type == "IHDR")
            {
                width = ReadInt(file, pos + 8);
                height = ReadInt(file, pos + 12);
                bitDepth = file[pos + 16];
                colorType = file[pos + 17];
            }
            else if (type == "IDAT")
            {
                compressed.Write(file, pos + 8, length);
            }
            pos += 12 + length;
        }

        if (bitDepth != 16 || (colorType != 0 && colorType != 6)) throw new InvalidDataException($"16비트 그레이/RGBA PNG만 지원한다(bitDepth {bitDepth}, colorType {colorType}).");
        int bytesPerPixel = colorType == 0 ? 2 : 8;
        int stride = width * bytesPerPixel;

        ushort[] samples = new ushort[width * height];
        compressed.Position = 2; // zlib 헤더 2바이트를 건너뛴다.
        using (DeflateStream inflate = new DeflateStream(compressed, CompressionMode.Decompress))
        {
            byte[] previous = new byte[stride];
            byte[] current = new byte[stride];
            for (int y = 0; y < height; y++)
            {
                int filter = inflate.ReadByte();
                ReadExact(inflate, current, stride);
                Unfilter(filter, current, previous, bytesPerPixel);
                for (int x = 0; x < width; x++)
                    samples[y * width + x] = (ushort)((current[x * bytesPerPixel] << 8) | current[x * bytesPerPixel + 1]);
                byte[] swap = previous; previous = current; current = swap;
            }
        }

        float[,] result = new float[outRes, outRes];
        float step = (cropSize - 1f) / (outRes - 1f);
        for (int z = 0; z < outRes; z++)
        {
            float sy = Mathf.Clamp(cropY + z * step, 0f, height - 1.001f);
            int y0 = (int)sy; float fy = sy - y0;
            for (int x = 0; x < outRes; x++)
            {
                float sx = Mathf.Clamp(cropX + x * step, 0f, width - 1.001f);
                int x0 = (int)sx; float fx = sx - x0;
                float a = samples[y0 * width + x0], b = samples[y0 * width + x0 + 1];
                float c = samples[(y0 + 1) * width + x0], d = samples[(y0 + 1) * width + x0 + 1];
                result[z, x] = Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy) / 65535f;
            }
        }
        return result;
    }

    private static int ReadInt(byte[] data, int offset) => (data[offset] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3];

    private static void ReadExact(Stream stream, byte[] buffer, int count)
    {
        int read = 0;
        while (read < count)
        {
            int n = stream.Read(buffer, read, count - read);
            if (n <= 0) throw new EndOfStreamException("PNG 데이터가 짧다.");
            read += n;
        }
    }

    private static void Unfilter(int filter, byte[] row, byte[] previous, int bpp)
    {
        int n = row.Length;
        switch (filter)
        {
            case 1:
                for (int i = bpp; i < n; i++) row[i] = (byte)(row[i] + row[i - bpp]);
                break;
            case 2:
                for (int i = 0; i < n; i++) row[i] = (byte)(row[i] + previous[i]);
                break;
            case 3:
                for (int i = 0; i < n; i++) row[i] = (byte)(row[i] + (((i >= bpp ? row[i - bpp] : 0) + previous[i]) >> 1));
                break;
            case 4:
                for (int i = 0; i < n; i++)
                {
                    int a = i >= bpp ? row[i - bpp] : 0;
                    int b = previous[i];
                    int c = i >= bpp ? previous[i - bpp] : 0;
                    int p = a + b - c;
                    int pa = Math.Abs(p - a), pb = Math.Abs(p - b), pc = Math.Abs(p - c);
                    int predictor = (pa <= pb && pa <= pc) ? a : (pb <= pc ? b : c);
                    row[i] = (byte)(row[i] + predictor);
                }
                break;
        }
    }
}
