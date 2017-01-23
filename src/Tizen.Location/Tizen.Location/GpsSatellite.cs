/*
 * Copyright (c) 2016 Samsung Electronics Co., Ltd All Rights Reserved
 *
 * Licensed under the Apache License, Version 2.0 (the License);
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an AS IS BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Tizen.Location
{
    /// <summary>
    /// A class which contains the functionality for obtaining information about Gps satellites in range and in use.
    /// </summary>
    public class GpsSatellite
    {
        private int _interval = 1;
        private Locator _locator;
        private EventHandler<SatelliteStatusChangedEventArgs> _satelliteStatusChanged;
        private IntPtr _handle = IntPtr.Zero;

        private Interop.GpsSatellite.SatelliteStatuschangedCallback _satelliteStatuschangedCallback;
        private Interop.GpsSatellite.SatelliteStatusinfomationCallback _satelliteStatusinfomationCallback;

        /// <summary>
        /// The time interval between callback updates.
        /// Should be in the range [1~120] seconds.
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when an invalid argument is used</exception>
        /// <exception cref="NotSupportedException">Thrown when the location is not supported</exception>
        public int Interval
        {
            get
            {
                Log.Info(Globals.LogTag, "Getting the Callback Interval");
                return _interval;
            }
            set
            {
                Log.Info(Globals.LogTag, "Setting the Callback Interval");
                if (value > 0 && value <= 120)
                {
                    _interval = value;
                }
                else
                {
                    Log.Error(Globals.LogTag, "Error Setting the Callback Interval");
                    throw LocationErrorFactory.ThrowLocationException((int)LocationError.InvalidParameter);
                }
            }
        }

        /// <summary>
        /// The NMEAData from the Satellite.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the operation is invalid for the current state</exception>
        /// <exception cref="ArgumentException">Thrown when an invalid argument is used</exception>
        /// <exception cref="UnauthroizedAccessException">Thrown when the app has no privilege to use the location</exception>
        /// <exception cref="NotSupportedException">Thrown when the location is not supported</exception>
        public string Nmea
        {
            get
            {
                Log.Info(Globals.LogTag, "Getting NMEAData");
                return GetNmea();
            }
        }

        private string GetNmea()
        {
            string value = null;
            int ret = Interop.GpsSatellite.GetNMEAData(_handle, out value);
            if (((LocationError)ret != LocationError.None))
            {
                Log.Error(Globals.LogTag, "Error getting the NMEAData," + (LocationError)ret);
                throw LocationErrorFactory.ThrowLocationException(ret);
            }

            return value;
        }


        /// <summary>
        /// The Count of Active satellites.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the operation is invalid for the current state</exception>
        /// <exception cref="ArgumentException">Thrown when an invalid argument is used</exception>
        /// <exception cref="UnauthroizedAccessException">Thrown when the app has no privilege to use the location</exception>
        /// <exception cref="NotSupportedException">Thrown when the location is not supported</exception>
        public int ActiveCount
        {
            get
            {
                return (int)GetActiveCount();
            }
        }

        private uint GetActiveCount()
        {
            Log.Info(Globals.LogTag, "Getting the ActiveCount of satellites");
            uint numActive = 0;
            uint numInView;
            int timestamp;
            int ret = Interop.GpsSatellite.GetSatelliteStatus(_handle, out numActive, out numInView, out timestamp);
            if (((LocationError)ret != LocationError.None))
            {
                Log.Error(Globals.LogTag, "Error getting the satellite" + (LocationError)ret);
                throw LocationErrorFactory.ThrowLocationException(ret);
            }
            return numActive;
        }

        /// <summary>
        /// The Count of satellites in view.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the operation is invalid for the current state</exception>
        /// <exception cref="ArgumentException">Thrown when an invalid argument is used</exception>
        /// <exception cref="UnauthroizedAccessException">Thrown when the app has no privilege to use the location</exception>
        /// <exception cref="NotSupportedException">Thrown when the location is not supported</exception>
        public int InViewCount
        {
            get
            {
                return (int)GetInViewCount();
            }
        }

        private uint GetInViewCount()
        {
            Log.Info(Globals.LogTag, "Getting the In view count of satellites");
            uint numActive;
            uint numInView = 0;
            int timestamp;
            int ret = Interop.GpsSatellite.GetSatelliteStatus(_handle, out numActive, out numInView, out timestamp);
            if (((LocationError)ret != LocationError.None))
            {
                Log.Error(Globals.LogTag, "Error getting the satellite" + (LocationError)ret);
                throw LocationErrorFactory.ThrowLocationException(ret);
            }
            return numInView;
        }

        /// <summary>
        /// The list of satellites/last recorded satellites in view.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the operation is invalid for the current state</exception>
        /// <exception cref="ArgumentException">Thrown when an invalid argument is used</exception>
        /// <exception cref="UnauthroizedAccessException">Thrown when the app has no privilege to use the location</exception>
        /// <exception cref="NotSupportedException">Thrown when the location is not supported</exception>
        public IList<SatelliteInformation> Satellites
        {
            get
            {
                return GetSatellites();
            }
        }

        private IList<SatelliteInformation> GetSatellites()
        {
            List<SatelliteInformation> satelliteList = new List<SatelliteInformation>();
            Log.Info(Globals.LogTag, "Getting the list of satellites");

            if (_satelliteStatusinfomationCallback == null)
            {
                _satelliteStatusinfomationCallback = (azimuth, elevation, prn, snr, isActive, userData) =>
                {
                    SatelliteInformation satellite = new SatelliteInformation(azimuth, elevation, prn, snr, isActive);
                    satelliteList.Add(satellite);
                    return true;
                };
            }

            int ret = Interop.GpsSatellite.GetForEachSatelliteInView(_handle, _satelliteStatusinfomationCallback, IntPtr.Zero);
            if (((LocationError)ret != LocationError.None))
            {
                Log.Error(Globals.LogTag, "Error getting the satellite" + (LocationError)ret);
                throw LocationErrorFactory.ThrowLocationException(ret);
            }
            return satelliteList;
        }

        /// <summary>
        /// The constructor of GpsSatellite class.
        /// <param name="locator"> Locator object initilized using Gps.</param>
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when an invalid argument is used</exception>
        public GpsSatellite(Locator locator)
        {
            Log.Info(Globals.LogTag, "Calling GpsSatellite constructor");
            LocationType method = locator.LocationType;
            if (method.Equals(LocationType.Gps) || method.Equals(LocationType.Hybrid))
            {
                _locator = locator;
                _handle = _locator.GetHandle();
            }
            else
            {
                Log.Error(Globals.LogTag, "Error constructing GpsSatellite class");
                throw LocationErrorFactory.ThrowLocationException((int)LocationError.InvalidParameter);
            }
        }

        /// <summary>
        /// (event) SatelliteStatusUpdated is raised whenever satellite information is updated.
        /// The callback will be invoked periodically (every Interval seconds).
        /// </summary>
        /// <exception cref="ArgumentException">Thrown when an invalid argument is used</exception>
        /// <exception cref="UnauthroizedAccessException">Thrown when the app has no privilege to use the location</exception>
        /// <exception cref="NotSupportedException">Thrown when the location is not supported</exception>
        public event EventHandler<SatelliteStatusChangedEventArgs> SatelliteStatusUpdated
        {
            add
            {
                Log.Info(Globals.LogTag, "SatelliteStatusUpdated Add called");
                if (_satelliteStatusChanged == null)
                {
                    Log.Info(Globals.LogTag, "SetSatelliteStatusChangeCallback called");
                    SetSatelliteStatusChangeCallback();
                }
                _satelliteStatusChanged += value;
            }
            remove
            {
                Log.Info(Globals.LogTag, "SatelliteStatusUpdated Remove called");
                _satelliteStatusChanged -= value;
                if (_satelliteStatusChanged == null)
                {
                    Log.Info(Globals.LogTag, "UnSetSatelliteStatusChangeCallback called");
                    UnSetSatelliteStatusChangeCallback();
                }
            }
        }

        private void SetSatelliteStatusChangeCallback()
        {
            Log.Info(Globals.LogTag, "SetSatelliteStatusChangeCallback");
            if (_satelliteStatuschangedCallback == null)
            {
                _satelliteStatuschangedCallback = (numActive, numInView, timestamp, userData) =>
                {
                    Log.Info(Globals.LogTag, "Inside SatelliteStatusChangedCallback");
                    DateTime timeStamp = DateTime.Now;

                    if (timestamp != 0)
                    {
                        DateTime start = DateTime.SpecifyKind(new DateTime(1970, 1, 1).AddSeconds(timestamp), DateTimeKind.Utc);
                        timeStamp = start.ToLocalTime();
                    }
                    _satelliteStatusChanged?.Invoke(_handle, new SatelliteStatusChangedEventArgs(numActive, numInView, timeStamp));
                };
            }

            GCHandle handle = GCHandle.Alloc(this);
            int ret = Interop.GpsSatellite.SetSatelliteStatusChangedCallback(_handle, _satelliteStatuschangedCallback, _interval, GCHandle.ToIntPtr(handle));
            if (((LocationError)ret != LocationError.None))
            {
                Log.Error(Globals.LogTag, "Error in setting satellite status changed callback," + (LocationError)ret);
                throw LocationErrorFactory.ThrowLocationException(ret);
            }
        }

        private void UnSetSatelliteStatusChangeCallback()
        {
            Log.Info(Globals.LogTag, "UnSetSatelliteStatusChangeCallback");
            int ret = Interop.GpsSatellite.UnSetSatelliteStatusChangedCallback(_handle);
            if (((LocationError)ret != LocationError.None))
            {
                Log.Error(Globals.LogTag, "Error in Getting Unsetting satellite status changed callback," + (LocationError)ret);
                throw LocationErrorFactory.ThrowLocationException(ret);
            }
        }
    }

    /// <summary>
    /// A class which contains the information of the Satellite under consideration.
    /// </summary>
    public class SatelliteInformation
    {
        /// <summary>
        /// Class Constructor for SatelliteInformation class.
        /// </summary>
        /// <param name="azimuth"> The azimuth value of the satellite in degrees.</param>
        /// <param name="elevation"> The elevation of the satellite in meters.</param>
        /// <param name="prn"> The Prn value of the satellite.</param>
        /// <param name="snr"> The SNR value of the satellite in dB.</param>
        /// <param name="isActive"> The flag signaling if satellite is in use.</param>
        public SatelliteInformation(uint azimuth, uint elevation, uint prn, uint snr, bool isActive)
        {
            Azimuth = azimuth;
            Elevation = elevation;
            Prn = prn;
            Snr = snr;
            Active = isActive;
        }

        /// <summary>
        /// The Azimuth information of the Satellite.
        /// </summary>
        public uint Azimuth { get; private set; }

        /// <summary>
        /// The Elevation information of the Satellite.
        /// </summary>
        public uint Elevation { get; private set; }

        /// <summary>
        /// The PRN of the Satellite.
        /// </summary>
        public uint Prn { get; private set; }

        /// <summary>
        /// The SNR of the Satellite.
        /// </summary>
        public uint Snr { get; private set; }

        /// <summary>
        /// The operational status of the Satellite.
        /// </summary>
        public bool Active { get; private set; }
    }
}
