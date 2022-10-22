using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;


namespace ImagesSorter
{
    class ImageSorter
    {
        class DictionaryState
        {
            public int MaxId;
        }

        class SortedData
        {
            public IDictionary<string, List<string>> FilesWithTime = new Dictionary<string, List<string>>();
        }

        readonly string _directory;
        readonly IDictionary<string, DictionaryState> _dictionaries = new Dictionary<string, DictionaryState>();
        readonly IDictionary<string, SortedData> _filesData = new Dictionary<string, SortedData>();

        public ImageSorter(string directory)
        {
            _directory = directory;
        }

        public void PerformSorting(bool splitByDateToDirectory)
        {
            if (splitByDateToDirectory)
            {
                RetrieveDirectoriesData();
                RetrieveFileData();
                MoveFiles();
            }
            else
            {
                RetrieveFileData();
                RenameFiles();
            }
        }

        void RenameFiles()
        {
            foreach (var data in _filesData)
            {
                var id = GetLastConvertedId(_directory, data.Key);
                foreach (var files in data.Value.FilesWithTime)
                {
                    foreach (var file in files.Value)
                    {
                        var name = Path.GetFileNameWithoutExtension(file);
                        if (IsConvertedFileName(name)) continue;
                        var srcPath = Path.Combine(_directory, file);
                        var destPath = string.Format("{0}\\{1}_{2:000}{3}", _directory, data.Key, ++id, Path.GetExtension(file));
                        File.Move(srcPath, destPath);
                    }
                }
            }
        }

        bool IsConvertedFileName(string name)
        {
            //2012.08.01_000
            if (name.Length < 14) return false;
            int number;
            if (!int.TryParse(name.Substring(0, 4), out number)) return false;
            if (name[4] != '.') return false;
            if (!int.TryParse(name.Substring(5, 2), out number)) return false;
            if (name[7] != '.') return false;
            if (!int.TryParse(name.Substring(8, 2), out number)) return false;
            if (name[10] != '_') return false;
            if (!int.TryParse(name.Substring(11), out number)) return false;
            return true;
        }

        int GetLastConvertedId(string directory, string prefix)
        {
            var files = Directory.GetFiles(directory, prefix + "_*");
            Array.Sort(files);
            for (var i = files.Length - 1; i >= 0; --i)
            {
                var lastFileName = Path.GetFileNameWithoutExtension(files[i]);
                var restNumber = lastFileName.Substring(prefix.Length + 1);
                int number;
                if (int.TryParse(restNumber, out number))
                    return number;
            }
            return 0;
        }

        void MoveFiles()
        {
            foreach (var data in _filesData)
            {
                DictionaryState directory;
                if (!_dictionaries.TryGetValue(data.Key, out directory))
                {
                    _dictionaries[data.Key] = directory = new DictionaryState { MaxId = 0 };
                    Directory.CreateDirectory(string.Format("{0}\\{1}", _directory, data.Key));
                }
                foreach (var files in data.Value.FilesWithTime)
                {
                    foreach (var file in files.Value)
                    {
                        var srcPath = Path.Combine(_directory, file);
                        directory.MaxId++;
                        var destPath = string.Format("{0}\\{1}\\{1}_{2:000}{3}", _directory, data.Key, directory.MaxId,
                                                      Path.GetExtension(file));
                        File.Move(srcPath, destPath);
                    }
                }
            }
        }

        bool IsJPEGFile(string path)
        {
            try
            {
                using (var stream = File.OpenRead(path))
                {
                    var buffer = new byte[11];
                    if (stream.Read(buffer, 0, 11) != 11)
                        return false;
                    return buffer[0] == 0xFF && buffer[1] == 0xD8 && buffer[2] == 0xFF && buffer[10] == 0x00;
                }
            }
            catch
            {
            }
            return false;
        }

        void RetrieveFileData()
        {
            _filesData.Clear();
            var files = Directory.GetFiles(_directory);

            foreach (var file in files)
            {
                if (!IsJPEGFile(file))
                    continue;
                try
                {
                    using (var bitmap = new Bitmap(file))
                    {
                        var exifExtractor = new ExifExtractor(bitmap, Environment.NewLine);
                        var dateExif = exifExtractor["Date Time"];
                        if (dateExif == null || !(dateExif is string) || ((string)dateExif).Length < 20)
                        {
                            throw new ArgumentException("Data time is not valid.");
                        }
                        var date = GetNormalizeDate(((string)dateExif).Substring(0, 10));
                        var time = GetNormalizeTime(((string)dateExif).Substring(11, 9));
                        SortedData data;
                        if (!_filesData.TryGetValue(date, out data))
                        {
                            _filesData[date] = data = new SortedData();
                        }
                        List<string> ffiles;
                        if (!data.FilesWithTime.TryGetValue(time, out ffiles))
                        {
                            data.FilesWithTime[time] = ffiles = new List<string>();
                        }
                        ffiles.Add(Path.GetFileName(file));
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("File info {0} could not be retrieved.{1}", file, ex);
                }
            }
        }

        static string GetNormalizeDate(string date)
        {
            var yearString = date.Substring(0, 4);
            var monthString = date.Substring(5, 2);
            var dayString = date.Substring(8, 2);
            int year, month, day;
            if (!int.TryParse(yearString, out year) || !int.TryParse(monthString, out month) || !int.TryParse(dayString, out day))
            {
                throw new ArgumentException("Data is not valid.");
            }
            return string.Format("{0:0000}.{1:00}.{2:00}", year, month, day);
        }

        static string GetNormalizeTime(string time)
        {
            var hourString = time.Substring(0, 2);
            var minuteString = time.Substring(3, 2);
            var secondString = time.Substring(6, 2);
            int hour, minute, second;
            if (!int.TryParse(hourString, out hour) || !int.TryParse(minuteString, out minute) || !int.TryParse(secondString, out second))
            {
                throw new ArgumentException("Time is not valid.");
            }
            return string.Format("{0:00}.{1:00}.{2:00}", hour, minute, second);
        }

        void RetrieveDirectoriesData()
        {
            _dictionaries.Clear();
            var directories = Directory.GetDirectories(_directory);
            foreach (var directory in directories)
            {
                var maxId = GetLastConvertedId(directory, Path.GetFileName(directory));
                _dictionaries[Path.GetFileName(directory)] = new DictionaryState { MaxId = maxId };
            }
        }
    }
}
