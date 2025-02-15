﻿/*
This file is part of PacketDotNet

PacketDotNet is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

PacketDotNet is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with PacketDotNet.  If not, see <http://www.gnu.org/licenses/>.
*/
/*
 *  Copyright 2017 Andrew <pandipd@outlook.com>
 */

using System;
using System.Collections.Generic;
using System.Threading;
using PacketDotNet.Utils;
using PacketDotNet.Utils.Converters;

#if DEBUG
using log4net;
using System.Reflection;
#endif

namespace PacketDotNet
{
    /// <summary>
    /// DrdaPacket
    /// See: https://en.wikipedia.org/wiki/Distributed_Data_Management_Architecture
    /// </summary>
    public sealed class DrdaPacket : Packet
    {
#if DEBUG
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
#else
// NOTE: No need to warn about lack of use, the compiler won't
//       put any calls to 'log' here but we need 'log' to exist to compile
#pragma warning disable 0169, 0649
        private static readonly ILogInactive Log;
#pragma warning restore 0169, 0649
#endif

        private List<DrdaDdmPacket> _ddmList;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="byteArraySegment"></param>
        public DrdaPacket(ByteArraySegment byteArraySegment)
        {
            Log.Debug("");

            // set the header field, header field values are retrieved from this byte array
            Header = new ByteArraySegment(byteArraySegment);

            // store the payload bytes
            PayloadPacketOrData = new Lazy<PacketOrByteArraySegment>(() =>
                                                                     {
                                                                         var result = new PacketOrByteArraySegment { ByteArraySegment = Header.NextSegment() };
                                                                         return result;
                                                                     },
                                                                     LazyThreadSafetyMode.PublicationOnly);
        }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="byteArraySegment"></param>
        /// <param name="parentPacket"></param>
        public DrdaPacket(ByteArraySegment byteArraySegment, Packet parentPacket) : this(byteArraySegment)
        {
            Log.DebugFormat("ParentPacket.GetType() {0}", parentPacket.GetType());

            ParentPacket = parentPacket;
        }

        /// <summary>
        /// Decoded DDM Packet as a list.
        /// </summary>
        public List<DrdaDdmPacket> DrdaDdmPackets
        {
            get
            {
                if (_ddmList == null)
                {
                    _ddmList = new List<DrdaDdmPacket>();
                }

                if (_ddmList.Count > 0) return _ddmList;


                var startOffset = Header.Offset;
                while (startOffset < Header.BytesLength)
                {
                    var length = EndianBitConverter.Big.ToUInt16(Header.Bytes, startOffset);
                    if (startOffset + length <= Header.BytesLength)
                    {
                        var ddmBas = new ByteArraySegment(Header.Bytes, startOffset, length);
                        _ddmList.Add(new DrdaDdmPacket(ddmBas, this));
                    }

                    startOffset += length;
                }

                Log.DebugFormat("DrdaDdmPacket.Count {0}", _ddmList.Count);
                return _ddmList;
            }
        }
    }
}