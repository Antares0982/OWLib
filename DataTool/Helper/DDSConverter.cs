﻿using System;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using DirectXTexNet;

namespace DataTool.Helper {
    public static class DDSConverter {
        
        [Flags]
        public enum CoInit : uint {
            MultiThreaded = 0x00,
            ApartmentThreaded = 0x02,
            DisableOLE1DDE = 0x04,
            SpeedOverMemory = 0x08
        }

        [DllImport("Ole32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int CoInitializeEx([In, Optional] IntPtr pvReserved, [In] CoInit dwCoInit);
        

        public static unsafe byte[] ConvertDDS(Stream ddsSteam, DXGI_FORMAT targetFormat, ImageFormat imageFormat, int frame) {
            try {
                CoInitializeEx(IntPtr.Zero, CoInit.MultiThreaded | CoInit.SpeedOverMemory);

                byte[] data = new byte[ddsSteam.Length];
                ddsSteam.Read(data, 0, data.Length);
                ScratchImage scratch = null;
                try {
                    fixed (byte* dataPin = data) {
                        scratch = TexHelper.Instance.LoadFromDDSMemory((IntPtr) dataPin, data.Length, DDS_FLAGS.NONE);
                        TexMetadata info = scratch.GetMetadata();
                        if (TexHelper.Instance.IsCompressed(info.Format)) {
                            ScratchImage temp = scratch.Decompress(frame, DXGI_FORMAT.UNKNOWN);
                            scratch.Dispose();
                            scratch = temp;
                        }

                        info = scratch.GetMetadata();

                        if (info.Format != targetFormat) {
                            ScratchImage temp = scratch.Convert(targetFormat, TEX_FILTER_FLAGS.DEFAULT, 0.5f);
                            scratch.Dispose();
                            scratch = temp;
                        }

                        UnmanagedMemoryStream stream = null;
                        WICCodecs codec = WICCodecs.PNG;
                        bool isMultiFrame = false;
                        if (imageFormat.Equals(ImageFormat.Bmp)) {
                            codec = WICCodecs.BMP;
                        } else if (imageFormat.Equals(ImageFormat.Gif)) {
                            codec = WICCodecs.GIF;
                            isMultiFrame = true;
                        } else if (imageFormat.Equals(ImageFormat.Jpeg)) {
                            codec = WICCodecs.JPEG;
                        } else if (imageFormat.Equals(ImageFormat.Png)) {
                            codec = WICCodecs.PNG;
                        } else if (imageFormat.Equals(ImageFormat.Tiff)) {
                            codec = WICCodecs.TIFF;
                        } 

                        if (frame < 0) {
                            if (!isMultiFrame) {
                                frame = 0;
                            } else {
                                stream = scratch.SaveToWICMemory(0, info.ArraySize, WIC_FLAGS.ALL_FRAMES, TexHelper.Instance.GetWICCodec(codec));
                            }
                        }

                        if (frame >= 0) {
                            stream = scratch.SaveToWICMemory(frame, WIC_FLAGS.NONE, TexHelper.Instance.GetWICCodec(codec));
                        }

                        if (stream == null) {
                            return null;
                        }

                        byte[] tex = new byte[stream.Length];
                        stream.Read(tex, 0, tex.Length);
                        scratch.Dispose();
                        return tex;
                    }
                } catch {
                    if (scratch != null && scratch.IsDisposed == false) {
                        scratch.Dispose();
                    }
                }
            } catch {
                // ignored
            }

            return null;
        }
    }
}