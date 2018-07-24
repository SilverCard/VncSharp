// VncSharp.WPF
// Copyright (C) 2018 Ricardo Brito 
// Copyright (C) 2011 Masanori Nakano (Modified VncSharp for WPF)
// Copyright (C) 2008 David Humphrey
//
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA

using System;
using System.Drawing;

namespace VncSharp.WPF
{
    /// <summary>
    /// A clipped version of VncDesktopTransformPolicy.
    /// </summary>
    public sealed class VncWpfDesktopPolicy : VncDesktopTransformPolicy
	{
        public VncWpfDesktopPolicy(VncClient vnc, VncViewerControl remoteDesktop)
            : base(vnc, remoteDesktop)
        {
            if (vnc == null) throw new ArgumentNullException(nameof(vnc));
            if (remoteDesktop == null) throw new ArgumentNullException(nameof(remoteDesktop));
        }

        public override bool AutoScroll => true; 

        public override Size AutoScrollMinSize {
            get {
                if (_Vnc != null && _Vnc.Framebuffer != null) {
                    return new Size(_Vnc.Framebuffer.Width, _Vnc.Framebuffer.Height);
                } else {
                    return new Size(100, 100);
                }
            }
        }

        public override Point UpdateRemotePointer(Point current)
        {
            Point adjusted = new Point();

            adjusted.X = (int)((double)current.X / _ViewerControl.ImageScale);
            adjusted.Y = (int)((double)current.Y / _ViewerControl.ImageScale);

            return adjusted;
        }

        public override Rectangle AdjustUpdateRectangle(Rectangle updateRectangle)
        {
			int x, y;


            if (_ViewerControl.ActualWidth > _ViewerControl.VncImage.ActualWidth)
            {
                x = updateRectangle.X + (int)(_ViewerControl.ActualWidth - _ViewerControl.VncImage.ActualWidth) / 2;
            }
            else
            {
                x = updateRectangle.X;
            }

            if (_ViewerControl.ActualHeight > _ViewerControl.VncImage.ActualHeight)
            {
                y = updateRectangle.Y + (int)(_ViewerControl.ActualHeight - _ViewerControl.VncImage.ActualHeight) / 2;
            }
            else
            {
                y = updateRectangle.Y;
            }

			return new Rectangle(x, y, updateRectangle.Width, updateRectangle.Height);
        }

        public override Point GetMouseMovePoint(Point current) => UpdateRemotePointer(current);
    }
}