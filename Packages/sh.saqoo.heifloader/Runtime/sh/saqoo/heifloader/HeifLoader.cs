using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using UnityEngine;

public class HeifLoader
{
    public enum HeifChroma
    {
        Undefined = 99,
        Monochrome = 0,
        Chroma420 = 1,
        Chroma422 = 2,
        Chroma444 = 3,
        InterleavedRGB = 10,
        InterleavedRGBA = 11,
        InterleavedRRGGBB_BE = 12,
        InterleavedRRGGBBAA_BE = 13,
        InterleavedRRGGBB_LE = 14,
        InterleavedRRGGBBAA_LE = 15
    }

    public enum HeifColorspace
    {
        Undefined = 99,
        YCbCr = 0,
        RGB = 1,
        Monochrome = 2
    }

    public enum HeifChannel
    {
        Y = 0,
        Cb = 1,
        Cr = 2,
        R = 3,
        G = 4,
        B = 5,
        Alpha = 6,
        Interleaved = 10
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct HeifError
    {
        public int code;
        public int subcode;
        public IntPtr message;
    }

    private const string LIBHEIF_DLL = "libheif";

    [DllImport(LIBHEIF_DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr heif_context_alloc();

    [DllImport(LIBHEIF_DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern void heif_context_free(IntPtr ctx);

    [DllImport(LIBHEIF_DLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern HeifError heif_context_read_from_file(IntPtr ctx, string filename, IntPtr options);

    [DllImport(LIBHEIF_DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern HeifError heif_context_get_primary_image_handle(IntPtr ctx, out IntPtr handle);

    [DllImport(LIBHEIF_DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern void heif_image_handle_release(IntPtr handle);

    [DllImport(LIBHEIF_DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern HeifError heif_decode_image(IntPtr handle, out IntPtr img, HeifColorspace colorspace, HeifChroma chroma, IntPtr decode_options);

    [DllImport(LIBHEIF_DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern void heif_image_release(IntPtr img);

    [DllImport(LIBHEIF_DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int heif_image_get_width(IntPtr img, HeifChannel channel);

    [DllImport(LIBHEIF_DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern int heif_image_get_height(IntPtr img, HeifChannel channel);

    [DllImport(LIBHEIF_DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr heif_image_get_plane_readonly(IntPtr img, HeifChannel channel, out int stride);

    [DllImport(LIBHEIF_DLL, CallingConvention = CallingConvention.Cdecl)]
    private static extern HeifError heif_context_read_from_memory_without_copy(IntPtr ctx, IntPtr mem, IntPtr size, IntPtr options);

    public static async Task<Texture2D> LoadFromFileAsync(string filePath, bool flipY = false, bool mipChain = true, bool linear = false, bool asNormalMap = false)
    {
        var fileData = await System.IO.File.ReadAllBytesAsync(filePath);
        return await LoadFromBytesAsync(fileData, flipY, mipChain, linear, asNormalMap);
    }

    public static Texture2D LoadFromFile(string filePath, bool flipY = false, bool mipChain = true, bool linear = false, bool asNormalMap = false)
    {
        var fileData = System.IO.File.ReadAllBytes(filePath);
        return LoadFromBytes(fileData, flipY, mipChain, linear, asNormalMap);
    }

    public static async Task<Texture2D> LoadFromBytesAsync(byte[] data, bool flipY = false, bool mipChain = true, bool linear = false, bool asNormalMap = false)
    {
        var pixelData = await Task.Run(() => DecodeHeifData(data, flipY, asNormalMap));
        return CreateTexture(pixelData, mipChain, linear);
    }

    public static Texture2D LoadFromBytes(byte[] data, bool flipY = false, bool mipChain = true, bool linear = false, bool asNormalMap = false)
    {
        var pixelData = DecodeHeifData(data, flipY, asNormalMap);
        return CreateTexture(pixelData, mipChain, linear);
    }

    public static (byte[] pixelData, int width, int height) DecodeHeifData(byte[] data, bool flipY, bool asNormalMap)
    {
        var context = heif_context_alloc();
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        var image = IntPtr.Zero;
        var imageHandle = IntPtr.Zero;
        try
        {
            var dataPtr = handle.AddrOfPinnedObject();
            var error = heif_context_read_from_memory_without_copy(context, dataPtr, new IntPtr(data.Length), IntPtr.Zero);
            CheckError(error);

            error = heif_context_get_primary_image_handle(context, out imageHandle);
            CheckError(error);

            error = heif_decode_image(imageHandle, out image, HeifColorspace.RGB, HeifChroma.InterleavedRGBA, IntPtr.Zero);
            CheckError(error);

            var width = heif_image_get_width(image, HeifChannel.Interleaved);
            var height = heif_image_get_height(image, HeifChannel.Interleaved);

            var pixelDataPtr = heif_image_get_plane_readonly(image, HeifChannel.Interleaved, out var stride);
            var pixelData = new byte[width * height * 4];
            var rowSize = width * 4;

            for (var y = 0; y < height; y++)
            {
                var srcY = flipY ? (height - 1 - y) : y;
                Marshal.Copy(pixelDataPtr + (srcY * stride), pixelData, y * rowSize, rowSize);
            }

            if (asNormalMap)
            {
                ConvertToNormalMap(pixelData);
            }

            return (pixelData, width, height);
        }
        finally
        {
            if (image != IntPtr.Zero)
            {
                heif_image_release(image);
            }
            if (imageHandle != IntPtr.Zero)
            {
                heif_image_handle_release(imageHandle);
            }
            handle.Free();
            heif_context_free(context);
        }
    }

    private static Texture2D CreateTexture((byte[] pixelData, int width, int height) pixelData, bool mipChain, bool linear)
    {
        var texture = new Texture2D(pixelData.width, pixelData.height, TextureFormat.RGBA32, mipChain, linear, createUninitialized: true);
        texture.SetPixelData(pixelData.pixelData, 0);
        texture.Apply(true, true);
        return texture;
    }

    private static void ConvertToNormalMap(byte[] pixelData)
    {
        for (int i = 0; i < pixelData.Length; i += 4)
        {
            var r = pixelData[i];
            var g = pixelData[i + 1];
            pixelData[i] = 0xff;
            pixelData[i + 1] = g;
            pixelData[i + 2] = g;
            pixelData[i + 3] = r;
        }
    }

    private static void CheckError(HeifError error)
    {
        if (error.code != 0) // heif_error_Ok
        {
            var message = Marshal.PtrToStringUTF8(error.message);
            throw new Exception($"HEIF Error: {message} (code: {error.code})");
        }
    }
}
