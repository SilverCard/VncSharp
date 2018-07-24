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
	/// A Design Mode version of VncDesktopTransformPolicy.
	/// </summary>
	public sealed class VncDesignModeDesktopPolicy : VncDesktopTransformPolicy
	{
        public VncDesignModeDesktopPolicy(VncViewerControl control)
            : base(null, control)
        {
            if (control == null) throw new ArgumentNullException(nameof(control));
        }

        public override bool AutoScroll => true;
        public override Size AutoScrollMinSize => new Size(608, 427);
        public override Point UpdateRemotePointer(Point current) => throw new NotImplementedException();
        public override Rectangle AdjustUpdateRectangle(Rectangle updateRectangle) => throw new NotImplementedException();
        public override Point GetMouseMovePoint(Point current) => throw new NotImplementedException();

    }
}