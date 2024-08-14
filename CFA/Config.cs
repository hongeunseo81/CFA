using CoPick.Logging;
using CoPick.Setting;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using CoPick.Plc;

namespace WheelHetergeneousInspectionSystem.Models
{
    [Serializable]
    public class Config : ICustomTypeDescriptor
    {
        [Browsable(false)]
        public int RecentlyUsedWheel { get; set; }
        [Browsable(false)]
        public Dictionary<string, Dictionary<Camera2DAttribute, string>> Camera2DConfigs { get; set; }
        [Browsable(false)]
        public Dictionary<string, Dictionary<PlcAttribute, string>> PlcConfigs { get; set; }
        public string Plc { get; set; }

        [LocalizedDescription("WheelName")]
        [LocalizedCategory("Common", 1, 2)]
        [Browsable(false)]
        public string WheelName { get; set; } = "unknown";
        [LocalizedCategory("Model")]
        public string ModelPath { get; set; } = "C:/";
        [LocalizedCategory("Model")]
        [Browsable(false)]
        public int ModelInputWidth { get; set; } = 384;
        [LocalizedCategory("Model")]
        [Browsable(false)]
        public int ModelInputHeight { get; set; } = 256;
        [LocalizedCategory("Model")]
        [Browsable(false)]
        public string DictionaryPath { get; set; } = "C:/";
        [LocalizedCategory("Model")]
        [Browsable(false)]
        public int DictionarySize { get; set; } = 512;
        [LocalizedCategory("Model")]
        public string InspectionImagePath { get; set; } = "./inspectionImages";
        [LocalizedCategory("Model")]
        [Browsable(false)]
        public int WheelCount { get; set; } = 5;

        [LocalizedCategory("Camera")]
        public string Camera2D { get; set; } = "Basler";


        private string _logPath = "./log";
        [Browsable(false)]
        public string LogPath
        {
            get => _logPath;
            set
            {
                try
                {
                    Path.GetFullPath(value);
                    _logPath = value;
                }
                catch (Exception) { }
            }
        }
        [Browsable(false)]
        public LogLevel MinimumFileLogLevel { get; set; } = LogLevel.Debug;
        [Browsable(false)]
        public LogLevel MinimumUiLogLevel { get; set; } = LogLevel.Debug;
        [Browsable(false)]
        public OperationMode StartMode { get; set; }
        [Browsable(false)]
        public long CameraMaxScanTime { get; set; } = 5000;
        [Browsable(false)]
        public string Language { get; set; } = "ko-KR";
        [Browsable(false)]
        public string DailyProductionResetTime { get; set; } = "00:00";
        [Browsable(false)]
        public string SensorType { get; set; } = "Pylon";
        [Browsable(false)]
        public string ModelType { get; set; } = "Akaze";
        [Browsable(false)]
        public DateTime StartTimeToGetNgList { get; set; } = DateTime.Now;

        public Config()
        {
            PlcConfigs = new Dictionary<string, Dictionary<PlcAttribute, string>>() { };
            Camera2DConfigs = new Dictionary<string, Dictionary<Camera2DAttribute, string>>();
            Camera2DConfigs["Basler"] = DefaultSettingLoader.Camera2Ds[Camera2DMaker.BASLER]();
            UpdatePropertyDescriptors();
        }

        [NonSerialized]
        private PropertyDescriptorCollection _pdColl;

        #region Implementation of ICustomTypeDescriptor
        public void UpdatePropertyDescriptors()
        {
            PropertyDescriptorCollection pdc = TypeDescriptor.GetProperties(this);
            PropertyDescriptor[] propertyDescriptorArray = typeof(Config).GetProperties()
                .Select(m => new WheelInspectionConfigPropertyDescriptor(pdc[m.Name], m.GetCustomAttributes(false).Cast<Attribute>().ToArray()))
                .ToArray();
            _pdColl = new PropertyDescriptorCollection(propertyDescriptorArray);
        }

        public AttributeCollection GetAttributes() => TypeDescriptor.GetAttributes(this, true);
        public string GetClassName() => TypeDescriptor.GetClassName(this, true);
        public string GetComponentName() => TypeDescriptor.GetComponentName(this, true);
        public TypeConverter GetConverter() => TypeDescriptor.GetConverter(this, true);
        public EventDescriptor GetDefaultEvent() => TypeDescriptor.GetDefaultEvent(this, true);
        public PropertyDescriptor GetDefaultProperty() => TypeDescriptor.GetDefaultProperty(this, true);
        public object GetEditor(Type editorBaseType) => TypeDescriptor.GetEditor(this, editorBaseType, true);
        public EventDescriptorCollection GetEvents() => TypeDescriptor.GetEvents(this, true);
        public EventDescriptorCollection GetEvents(Attribute[] attributes) => TypeDescriptor.GetEvents(this, attributes, true);
        public PropertyDescriptorCollection GetProperties() => _pdColl;
        public PropertyDescriptorCollection GetProperties(Attribute[] attributes) => _pdColl;
        public object GetPropertyOwner(PropertyDescriptor pd) => this;
        #endregion
    }

    public class WheelTypeAndName
    {
        public string WheelType { get; private set; }
        public string WheelName { get; private set; }

        public WheelTypeAndName(string wheelType, string wheelName)
        {
            WheelType = wheelType;
            WheelName = wheelName;
        }
    }

    public static class DefaultSettingLoader
    {
        public static Dictionary<Camera2DMaker, Func<Dictionary<Camera2DAttribute, string>>> Camera2Ds = new Dictionary<Camera2DMaker, Func<Dictionary<Camera2DAttribute, string>>>()
        {
            [Camera2DMaker.BASLER] = GetBaslerSettings,
        };

        public static Dictionary<LineLaserMaker, Func<Dictionary<LineLaserAttribute, string>>> LineLasers = new Dictionary<LineLaserMaker, Func<Dictionary<LineLaserAttribute, string>>>()
        {
            [LineLaserMaker.SMARTRAY] = GetSmartRaySettings,
        };

        private static Dictionary<Camera2DAttribute, string> GetBaslerSettings()
        {
            return new Dictionary<Camera2DAttribute, string>()
            {
                [Camera2DAttribute.IPAdr] = "192.168.178.221",
                [Camera2DAttribute.CameraResolutionRoiTopLeftX] = "0",
                [Camera2DAttribute.CameraResolutionRoiTopLeftY] = "0",
                [Camera2DAttribute.CameraResolutionRoiWidth] = "658",
                [Camera2DAttribute.CameraResolutionRoiHeight] = "492",
                [Camera2DAttribute.Exposure] = "1000",
                [Camera2DAttribute.MaxFPS] = "30",
                [Camera2DAttribute.Gain] = "0",
                [Camera2DAttribute.ImageFolderPath] = "./images",
                [Camera2DAttribute.FPS] = "15"
            };
        }

        private static Dictionary<LineLaserAttribute, string> GetSmartRaySettings()
        {
            return new Dictionary<LineLaserAttribute, string>()
            {
                [LineLaserAttribute.IPAdr] = "192.168.178.200",
                [LineLaserAttribute.CameraResolutionRoiTopLeftX] = "384",
                [LineLaserAttribute.CameraResolutionRoiTopLeftY] = "200",
                [LineLaserAttribute.CameraResolutionRoiWidth] = "1120",
                [LineLaserAttribute.CameraResolutionRoiHeight] = "800",
                [LineLaserAttribute.CamIndex] = "0",
                [LineLaserAttribute.CamName] = "unknown",
                [LineLaserAttribute.PortNum] = "40",
                [LineLaserAttribute.NumberOfExpectedProfiles] = "0",
                [LineLaserAttribute.PacketSize] = "1024",
                [LineLaserAttribute.PacketTimeOut] = "10",
                [LineLaserAttribute.Width] = "0",
                [LineLaserAttribute.ParamSetPath] = "ECCO85_Liveimage.par",
                [LineLaserAttribute.ExposureTime1MicroS] = "5000",
                [LineLaserAttribute.ExposureTime2MicroS] = "10000",
                [LineLaserAttribute.Gain] = "0",

                //offline
                [LineLaserAttribute.ImageFolderPath] = "C:/",
                [LineLaserAttribute.FPS] = "20"
            };
        }

        public static Dictionary<PlcModel, Func<Dictionary<PlcAttribute, string>>> Plcs = new Dictionary<PlcModel, Func<Dictionary<PlcAttribute, string>>>()
        {
            [PlcModel.S7] = GetSiemensSettings,
            [PlcModel.MELSEC] = GetMelsecSettings,
            [PlcModel.AB] = GetAbSvnSettings
        };

        private static Dictionary<PlcAttribute, string> GetMelsecSettings()
        {
            return new Dictionary<PlcAttribute, string>()
            {
                [PlcAttribute.LOGICAL_STATION] = "0"
            };
        }

        private static Dictionary<PlcAttribute, string> GetSiemensSettings()
        {
            return new Dictionary<PlcAttribute, string>()
            {
                [PlcAttribute.IP] = "192.168.178.5",
                [PlcAttribute.RACK] = "0",
                [PlcAttribute.SLOT] = "2",
                [PlcAttribute.WRITE_DB] = "100",
                [PlcAttribute.READ_DB] = "100"
            };
        }
        private static Dictionary<PlcAttribute, string> GetAbSvnSettings()
        {
            return new Dictionary<PlcAttribute, string>()
            {
                [PlcAttribute.Model] = PlcModel.AB.ToString(),
                [PlcAttribute.IP] = "192.168.1.200",
                [PlcAttribute.TagPrefix] = "PRIMER_VISION_",
                [PlcAttribute.HeartbeatTag] = "PRIMER_VISION_LIVE_BIT"
            };
        }
    }

    public class WheelInspectionConfigPropertyDescriptor : PropertyDescriptor
    {
        private PropertyDescriptor _originalPd;

        public WheelInspectionConfigPropertyDescriptor(PropertyDescriptor pd, Attribute[] attrs)
            : base(pd, attrs)
        {
            _originalPd = pd;
        }

        public override Type ComponentType
        {
            get => _originalPd.ComponentType;
        }
        public override bool IsReadOnly
        {
            get => _originalPd.IsReadOnly;
        }

        public override Type PropertyType
        {
            get => _originalPd.PropertyType;
        }

        public override bool CanResetValue(object component) => _originalPd.CanResetValue(component);
        public override object GetValue(object component) => _originalPd.GetValue(component);
        public override void ResetValue(object component) => _originalPd.ResetValue(component);
        public override void SetValue(object component, object value) => _originalPd.SetValue(component, value);
        public override bool ShouldSerializeValue(object component) => _originalPd.ShouldSerializeValue(component);
    }

}

