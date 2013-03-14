/*
 * Box Social™
 * http://boxsocial.net/
  * Copyright © 2007, David Smith
 * 
 * $Id:$
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License version 2 of
 * the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

namespace BoxSocial.IO
{
    // Adapted from http://www.toptensoftware.com/Articles/17/high-quality-image-resampling-in-monolinux
    public class ImageMagick
    {
        public enum Filter
        {
            Undefined,
            Point,
            Box,
            Triangle,
            Hermite,
            Hanning,
            Hamming,
            Blackman,
            Gaussian,
            Quadratic,
            Cubic,
            Catrom,
            Mitchell,
            Lanczos,
            Bessel,
            Sinc,
            Kaiser,
            Welsh,
            Parzen,
            Lagrange,
            Bohman,
            Bartlett,
            SincFast
        };

        public enum InterpolatePixel
        {
            Undefined,
            Average,
            Bicubic,
            Bilinear,
            Filter,
            Integer,
            Mesh,
            NearestNeighbor,
            Spline
        };

        // You may need to change this for your ImageMagick version
        [DllImport("libMagickWand.so.4", EntryPoint = "MagickResizeImage")]
        public static extern bool ResizeImage(IntPtr mgck_wand, IntPtr columns, IntPtr rows, Filter filter_type, double blur);

        [DllImport("libMagickWand.so.4", EntryPoint = "MagickCropImage")]
        public static extern bool CropImage(IntPtr mgck_wand, IntPtr columns, IntPtr rows, IntPtr x, IntPtr y);

        [DllImport("libMagickWand.so.4", EntryPoint = "MagickWandGenesis")]
        public static extern void WandGenesis();

        [DllImport("libMagickWand.so.4", EntryPoint = "MagickWandTerminus")]
        public static extern void WandTerminus();

        [DllImport("libMagickWand.so.4", EntryPoint = "NewMagickWand")]
        public static extern IntPtr NewWand();

        [DllImport("libMagickWand.so.4", EntryPoint = "DestroyMagickWand")]
        public static extern IntPtr DestroyWand(IntPtr wand);

        [DllImport("libMagickWand.so.4", EntryPoint = "MagickGetImageBlob")]
        public static extern IntPtr GetImageBlob(IntPtr wand, [Out] out IntPtr length);

        [DllImport("libMagickWand.so.4", EntryPoint = "MagickReadImageBlob")]
        public static extern bool ReadImageBlob(IntPtr wand, IntPtr blob, IntPtr length);

        [DllImport("libMagickWand.so.4", EntryPoint = "MagickRelinquishMemory")]
        public static extern IntPtr RelinquishMemory(IntPtr resource);

        [DllImport("libMagickWand.so.4", EntryPoint = "MagickGetImageWidth")]
        public static extern IntPtr GetWidth(IntPtr wand);

        [DllImport("libMagickWand.so.4", EntryPoint = "MagickGetImageHeight")]
        public static extern IntPtr GetHeight(IntPtr wand);

        // Interop
        public static bool ReadImageBlob(IntPtr wand, byte[] blob)
        {
            GCHandle pinnedArray = GCHandle.Alloc(blob, GCHandleType.Pinned);
            IntPtr pointer = pinnedArray.AddrOfPinnedObject();

            bool bRetv = ReadImageBlob(wand, pointer, (IntPtr)blob.Length);

            pinnedArray.Free();

            return bRetv;
        }

        // Interop
        public static byte[] GetImageBlob(IntPtr wand)
        {

            // Get the blob
            IntPtr len;
            IntPtr buf = GetImageBlob(wand, out len);

            // Copy it
            var dest = new byte[len.ToInt32()];
            Marshal.Copy(buf, dest, 0, len.ToInt32());

            // Relinquish
            RelinquishMemory(buf);

            return dest;
        }

    }
}
