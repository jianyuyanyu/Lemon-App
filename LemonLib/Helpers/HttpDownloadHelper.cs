﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Un4seen.Bass;

namespace LemonLib
{
    public class HttpDownloadHelper
    {
        private string Id = "";
        private string Path = "";
        public bool Downloading = true;
        private int Progress = 0;

        public delegate void Pr(int pr);
        public event Pr ProgressChanged;
        public delegate void finish();
        public event finish Finished;
        public delegate void x(string z);
        public event x GetSize;
        public HttpDownloadHelper(string id, string pa)
        {
            Id = id;
            Path = pa;
        }
        public async void Download()
        {
            await Task.Run(async () =>
            {
                string Url = await MusicLib.GetUrlAsync(Id);
                Console.WriteLine(Path + "  " + Downloading + "\r\n" + Url);
                HttpWebRequest Myrq = (HttpWebRequest)WebRequest.Create(Url);
                Myrq.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
                Myrq.Headers.Add("Accept-Language", "zh-CN,zh;q=0.9,en;q=0.8,en-GB;q=0.7,en-US;q=0.6");
                Myrq.Headers.Add("Cache-Control", "max-age=0");
                Myrq.KeepAlive = true;
                Myrq.Headers.Add("Cookie", Settings.USettings.Cookie);
                Myrq.Host = "aqqmusic.tc.qq.com";
                Myrq.Headers.Add("Upgrade-Insecure-Requests", "1");
                Myrq.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/80.0.3987.66 Safari/537.36 Edg/80.0.361.40";
                var myrp = (HttpWebResponse)Myrq.GetResponse();
                Console.WriteLine(myrp.StatusCode.ToString());
                var totalBytes = myrp.ContentLength;
                GetSize(Getsize(totalBytes));
                Stream st = myrp.GetResponseStream();
                Stream so = new FileStream(Path, FileMode.Create);
                long totalDownloadedByte = 0;
                byte[] by = new byte[1048576];
                int osize = await st.ReadAsync(by, 0, (int)by.Length);
                while (osize > 0)
                {
                    if (stop) break;
                    if (Downloading)
                    {
                        totalDownloadedByte = osize + totalDownloadedByte;
                        await so.WriteAsync(by, 0, osize);
                        osize = await st.ReadAsync(by, 0, (int)by.Length);
                        Progress = (int)((float)totalDownloadedByte / (float)totalBytes * 100);
                        ProgressChanged(Progress);
                        Console.WriteLine("downloading:" + Progress);
                    }
                }
                st.Close();
                so.Close();
                myrp.Close();
                if(!stop)Finished();
            });
        }
        public void Pause()
        {
            Downloading = false;
            Console.WriteLine("下载暂停");
        }
        public void Start()
        {
            Downloading = true;
            Console.WriteLine("Start.");
        }
        public bool stop = false;
        public void Stop() {
            stop = true;
        }
        private string Getsize(double size)
        {
            string[] units = new string[] { "B", "KB", "MB", "GB", "TB", "PB" };
            double mod = 1024.0;
            int i = 0;
            while (size >= mod)
            {
                size /= mod;
                i++;
            }
            return size.ToString("0.00") + units[i];
        }
    }
    public class BASSDL {
        public DOWNLOADPROC _myDownloadProc;
        private FileStream _fs = null;
        private byte[] _data; // local data buffer
        private string DLPath;
        public Action<long, long> procChanged = null;
        public Action finished = null;
        public int stream;
        public BASSDL(string path) {
            _myDownloadProc = new DOWNLOADPROC(DownloadCallBack);
            DLPath = path;
        }
        public void SetClose() {
            procChanged = null;
            finished = null;
        }
        private void DownloadCallBack(IntPtr buffer, int length, IntPtr user)
        {
            // file length
            long len = Bass.BASS_StreamGetFilePosition(stream, BASSStreamFilePosition.BASS_FILEPOS_END);
            // download progress
            long down = Bass.BASS_StreamGetFilePosition(stream, BASSStreamFilePosition.BASS_FILEPOS_DOWNLOAD);
            procChanged?.Invoke(len, down);
            //finished downloading
            if (_fs == null)
            {
                // create the file
                _fs = File.OpenWrite(DLPath);
            }
            if (buffer == IntPtr.Zero)
            {
                // finished downloading
                finished?.Invoke();
                _fs.Flush();
                _fs.Close();
                _fs = null;
                FileInfo fi = new FileInfo(DLPath);
                if (fi.Length != len)
                    fi.Delete();
            }
            else
            {
                // increase the data buffer as needed
                if (_data == null || _data.Length < length)
                    _data = new byte[length];
                // copy from managed to unmanaged memory
                Marshal.Copy(buffer, _data, 0, length);
                // write to file
                _fs.Write(_data, 0, length);
            }
        }
    }
}
