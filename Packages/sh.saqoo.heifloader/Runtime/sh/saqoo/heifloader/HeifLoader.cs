using System;
using System.Runtime.InteropServices;
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

    public static Texture2D LoadFromFile(string filePath, bool flipY = false, bool mipChain = true, bool linear = false, bool asNormalMap = false)
    {
        var fileData = System.IO.File.ReadAllBytes(filePath);
        return LoadFromBytes(fileData, flipY, mipChain, linear, asNormalMap);
    }

    public static Texture2D LoadFromBytes(byte[] data, bool flipY = false, bool mipChain = true, bool linear = false, bool asNormalMap = false)
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
            Marshal.Copy(pixelDataPtr, pixelData, 0, pixelData.Length);

            if (flipY)
            {
                FlipTextureVertically(pixelData, width, height);
            }

            if (asNormalMap)
            {
                ConvertToNormalMap(pixelData);
            }

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain, linear, createUninitialized: true);
            texture.SetPixelData(pixelData, 0);
            texture.Apply();

            return texture;
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

    private static void FlipTextureVertically(byte[] pixelData, int width, int height)
    {
        var bytesPerRow = width * 4;
        var tempRow = new byte[bytesPerRow];
        for (int y = 0; y < height / 2; y++)
        {
            var topRowStart = y * bytesPerRow;
            var bottomRowStart = (height - 1 - y) * bytesPerRow;
            Array.Copy(pixelData, topRowStart, tempRow, 0, bytesPerRow);
            Array.Copy(pixelData, bottomRowStart, pixelData, topRowStart, bytesPerRow);
            Array.Copy(tempRow, 0, pixelData, bottomRowStart, bytesPerRow);
        }
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
