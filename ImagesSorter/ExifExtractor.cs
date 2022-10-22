using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Collections;
using System.IO;


namespace ImagesSorter
{
    public class ExifExtractor : IEnumerable
    {
        private readonly System.Drawing.Bitmap _bitmap;
        private string _rawData;
        private readonly Translation _translation;
        private readonly Hashtable _properties;
        readonly string _dataSeparator;

        internal int Count { get { return _properties.Count; } }
        public object this[string index] { get { return _properties[index]; } }

        public ExifExtractor(System.Drawing.Bitmap bitmap, string dataSeparator)
        {
            _properties = new Hashtable();
            _bitmap = bitmap;
            _dataSeparator = dataSeparator;
            _translation = new Translation();
            BuildDB(_bitmap.PropertyItems);
        }

        public static PropertyItem[] GetExifProperties(string fileName)
        {
            var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            var image = System.Drawing.Image.FromStream(stream, true, false);
            return image.PropertyItems;
        }

        public ExifExtractor(string file, string dataSeparator)
        {
            _properties = new Hashtable();
            _dataSeparator = dataSeparator;
            _translation = new Translation();
            BuildDB(GetExifProperties(file));
        }

        private void BuildDB(IEnumerable<PropertyItem> parr)
        {
            _properties.Clear();
            _rawData = "";
            var ascii = Encoding.ASCII;
            foreach (var p in parr)
            {
                var v = "";
                var name = (string)_translation[p.Id];
                if (name == null) continue;
                _rawData += name + ": ";
                //1 = BYTE An 8-bit unsigned integer.,
                switch (p.Type)
                {
                    case 0x1:
                        v = p.Value[0].ToString(CultureInfo.InvariantCulture);
                        break;
                    case 0x2:
                        v = ascii.GetString(p.Value);
                        break;
                    case 0x3:
                        switch (p.Id)
                        {
                            case 0x8827: // ISO
                                v = "ISO-" + ConvertToInt16U(p.Value).ToString(CultureInfo.InvariantCulture);
                                break;
                            case 0xA217: // sensing method
                                {
                                    switch (ConvertToInt16U(p.Value))
                                    {
                                        case 1: v = "Not defined"; break;
                                        case 2: v = "One-chip color area sensor"; break;
                                        case 3: v = "Two-chip color area sensor"; break;
                                        case 4: v = "Three-chip color area sensor"; break;
                                        case 5: v = "Color sequential area sensor"; break;
                                        case 7: v = "Trilinear sensor"; break;
                                        case 8: v = "Color sequential linear sensor"; break;
                                        default: v = " reserved"; break;
                                    }
                                }
                                break;
                            case 0x8822: // aperture 
                                switch (ConvertToInt16U(p.Value))
                                {
                                    case 0: v = "Not defined"; break;
                                    case 1: v = "Manual"; break;
                                    case 2: v = "Normal program"; break;
                                    case 3: v = "Aperture priority"; break;
                                    case 4: v = "Shutter priority"; break;
                                    case 5: v = "Creative program (biased toward depth of field)"; break;
                                    case 6: v = "Action program (biased toward fast shutter speed)"; break;
                                    case 7: v = "Portrait mode (for closeup photos with the background out of focus)"; break;
                                    case 8: v = "Landscape mode (for landscape photos with the background in focus)"; break;
                                    default: v = "reserved"; break;
                                }
                                break;
                            case 0x9207: // metering mode
                                switch (ConvertToInt16U(p.Value))
                                {
                                    case 0: v = "unknown"; break;
                                    case 1: v = "Average"; break;
                                    case 2: v = "CenterWeightedAverage"; break;
                                    case 3: v = "Spot"; break;
                                    case 4: v = "MultiSpot"; break;
                                    case 5: v = "Pattern"; break;
                                    case 6: v = "Partial"; break;
                                    case 255: v = "Other"; break;
                                    default: v = "reserved"; break;
                                }
                                break;
                            case 0x9208: // light source
                                {
                                    switch (ConvertToInt16U(p.Value))
                                    {
                                        case 0: v = "unknown"; break;
                                        case 1: v = "Daylight"; break;
                                        case 2: v = "Fluorescent"; break;
                                        case 3: v = "Tungsten"; break;
                                        case 17: v = "Standard light A"; break;
                                        case 18: v = "Standard light B"; break;
                                        case 19: v = "Standard light C"; break;
                                        case 20: v = "D55"; break;
                                        case 21: v = "D65"; break;
                                        case 22: v = "D75"; break;
                                        case 255: v = "other"; break;
                                        default: v = "reserved"; break;
                                    }
                                }
                                break;
                            case 0x9209:
                                {
                                    switch (ConvertToInt16U(p.Value))
                                    {
                                        case 0: v = "Flash did not fire"; break;
                                        case 1: v = "Flash fired"; break;
                                        case 5: v = "Strobe return light not detected"; break;
                                        case 7: v = "Strobe return light detected"; break;
                                        default: v = "reserved"; break;
                                    }
                                }
                                break;
                            default:
                                v = ConvertToInt16U(p.Value).ToString(CultureInfo.InvariantCulture);
                                break;
                        }
                        break;
                    case 0x4:
                        v = ConvertToInt32U(p.Value).ToString(CultureInfo.InvariantCulture);
                        break;
                    case 0x5:
                        {
                            // rational
                            var n = new byte[p.Len / 2];
                            var d = new byte[p.Len / 2];
                            Array.Copy(p.Value, 0, n, 0, p.Len / 2);
                            Array.Copy(p.Value, p.Len / 2, d, 0, p.Len / 2);
                            var a = ConvertToInt32U(n);
                            var b = ConvertToInt32U(d);
                            var r = new Rational(a, b);
                            switch (p.Id)
                            {
                                case 0x9202: // aperture
                                    v = "F/" + Math.Round(Math.Pow(Math.Sqrt(2), r.ToDouble()), 2).ToString(CultureInfo.InvariantCulture);
                                    break;
                                case 0x920A:
                                    v = r.ToDouble().ToString(CultureInfo.InvariantCulture);
                                    break;
                                case 0x829A:
                                    v = r.ToDouble().ToString(CultureInfo.InvariantCulture);
                                    break;
                                case 0x829D: // F-number
                                    v = "F/" + r.ToDouble().ToString(CultureInfo.InvariantCulture);
                                    break;
                                default:
                                    v = r.ToString("/");
                                    break;
                            }

                        }
                        break;
                    case 0x7:
                        switch (p.Id)
                        {
                            case 0xA300:
                                {
                                    if (p.Value[0] == 3)
                                    {
                                        v = "DSC";
                                    }
                                    else
                                    {
                                        v = "reserved";
                                    }
                                    break;
                                }
                            case 0xA301:
                                v = p.Value[0] == 1 ? "A directly photographed image" : "Not a directly photographed image";
                                break;
                            default:
                                v = "-";
                                break;
                        }
                        break;
                    case 0x9:
                        v = ConvertToInt32(p.Value).ToString(CultureInfo.InvariantCulture);
                        break;
                    case 0xA:
                        {

                            // rational
                            var n = new byte[p.Len / 2];
                            var d = new byte[p.Len / 2];
                            Array.Copy(p.Value, 0, n, 0, p.Len / 2);
                            Array.Copy(p.Value, p.Len / 2, d, 0, p.Len / 2);
                            var a = ConvertToInt32(n);
                            var b = ConvertToInt32(d);
                            var r = new Rational(a, b);
                            switch (p.Id)
                            {
                                case 0x9201: // shutter speed
                                    v = "1/" + Math.Round(Math.Pow(2, r.ToDouble()), 2).ToString(CultureInfo.InvariantCulture);
                                    break;
                                case 0x9203:
                                    v = Math.Round(r.ToDouble(), 4).ToString(CultureInfo.InvariantCulture);
                                    break;
                                default:
                                    v = r.ToString("/");
                                    break;
                            }
                        }
                        break;
                }
                if (_properties[name] == null)
                    _properties.Add(name, v);
                _rawData += v;
                _rawData += _dataSeparator;
            }

        }

        public override string ToString()
        {
            return _rawData;
        }

        static int ConvertToInt32(byte[] arr)
        {
            if (arr.Length != 4)
                return 0;
            return arr[3] << 24 | arr[2] << 16 | arr[1] << 8 | arr[0];
        }

        static uint ConvertToInt32U(byte[] arr)
        {
            if (arr.Length != 4)
                return 0;
            return Convert.ToUInt32(arr[3] << 24 | arr[2] << 16 | arr[1] << 8 | arr[0]);
        }

        static uint ConvertToInt16U(byte[] arr)
        {
            if (arr.Length != 2)
                return 0;
            return Convert.ToUInt16(arr[1] << 8 | arr[0]);
        }

        public IEnumerator GetEnumerator()
        {
            return _properties.Cast<object>().GetEnumerator();
        }
    }

}
