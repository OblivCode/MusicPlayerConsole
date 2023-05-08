using NAudio.Wave;
using Newtonsoft.Json;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace MusicPlayerConsole
{
    public static class Global
    {
        public readonly static string[] SUPPORTED_FILE_EXTENSIONS = new string[3] { "mp3", "wav", "m4a" };
        readonly static string RESOURCES_PATH = Directory.GetCurrentDirectory() + "/resources";
        readonly static string CONFIG_FILENAME = RESOURCES_PATH + "/config.json";
        

        public static Config LoadConfig()
        {
            //Console.WriteLine(CONFIG_FILENAME);
            string json_text;
           try
            {
                json_text = File.ReadAllText(CONFIG_FILENAME);
            }
            catch (FileNotFoundException)
            {
                File.Create(CONFIG_FILENAME).Close();
                return LoadConfig();
            }
            catch(DirectoryNotFoundException)
            {
                Directory.CreateDirectory(RESOURCES_PATH);
                File.Create(CONFIG_FILENAME).Close();
                return LoadConfig();
            }
            
            Config? config = JsonConvert.DeserializeObject<Config>(json_text);
            if(config == null)
            {
                config = new Config();
                config.controls = new Controls()
                {
                    PreviousSound = ConsoleKey.Q,
                    NextSound = ConsoleKey.E,
                    PauseSound = ConsoleKey.F,
                    PlaySound = ConsoleKey.G,
                    Up = ConsoleKey.UpArrow,
                    Down = ConsoleKey.DownArrow,
                };
            }

            return config;
        }
        public static void SaveConfig(Config config)
        {
            string config_out = JsonConvert.SerializeObject(config);
            File.WriteAllText(CONFIG_FILENAME, config_out);
        }
        private static string FormatSoundName(string name)
        {
            string formatted = "";
            char[] bad_chars = new char[3] { '[', ']', '.' };

            foreach (char c in name)
            {
                if (!bad_chars.Contains(c))
                    formatted += c;
            }
            return formatted;
        }



        public struct Controls
        {
            public ConsoleKey PreviousSound;
            public ConsoleKey NextSound;
            public ConsoleKey PauseSound;
            public ConsoleKey PlaySound;
            public ConsoleKey Up;
            public ConsoleKey Down;
        }
        public class Config
        {
            public List<string> sound_paths { get; set; }
            public Controls controls { get; set; }
            public Config()
            {
                sound_paths = new List<string>();
            }
            public List<string> GetSoundPaths() => sound_paths;
            public int GetSoundPathsCount() => sound_paths.Count;
            public void SetSoundPaths(IEnumerable<string> sound_paths) => this.sound_paths = sound_paths.ToList();
            public void AddSoundPath(string path) => sound_paths.Add(path);
        }

        public class Sound
        {
            string FILENAME;

            public string GetName(bool with_extension = false)
            {
                if(with_extension)
                    return Path.GetFileName(FILENAME);
                else
                    return FormatSoundName(Path.GetFileNameWithoutExtension(FILENAME));
            }

            public string GetFilename() => FILENAME;

            public static Sound FromFile(string filename)
            {
                return new Sound(filename);
            }
            public Sound(string filename)
            {
                //Console.WriteLine(filename);
                FILENAME = filename;
                
            }
        }

        public class Playlist
        {
            AudioFileReader AUDIO_FILE_READER;
            WaveOutEvent WAVE_OUT_EVENT;
            LinkedList<Sound> LIST;
            public Grid GRID = new Grid();
            int INDEX_SELECTION = 0, INDEX_PLAYING = -1, MAX_PER_WINDOW = 25, WINDOW_NUM = 0;
            string FONT_NORMAL, FONT_SELECTED, FONT_PLAYING;
            bool PLAYING_SOUND = false;

            public Playlist(IEnumerable<Sound> playlist, (string, string, string) fonts)
            {
                LIST = new LinkedList<Sound>(playlist);
                FONT_NORMAL = fonts.Item1; FONT_SELECTED = fonts.Item2; FONT_PLAYING = fonts.Item3;

                

                Refresh();
            }

            public void Play(bool change = false)
            {
                if (PLAYING_SOUND)
                {
                    if (INDEX_SELECTION == INDEX_PLAYING)
                        return;
                    else if(INDEX_SELECTION != INDEX_PLAYING)
                    {
                        INDEX_PLAYING = INDEX_SELECTION;
                        WAVE_OUT_EVENT.Stop();
                    }
                }
                else if (INDEX_PLAYING == -1 || INDEX_SELECTION != INDEX_PLAYING)
                    INDEX_PLAYING = INDEX_SELECTION;

                if(change)
                {
                    Sound sound = LIST.ElementAt(INDEX_PLAYING);
                    string filename = sound.GetFilename();

                    AUDIO_FILE_READER = new AudioFileReader(filename);
                    WAVE_OUT_EVENT = new WaveOutEvent();
                    WAVE_OUT_EVENT.Init(AUDIO_FILE_READER);
                }

                WAVE_OUT_EVENT.Play();
                PLAYING_SOUND = true;
            }

            public void Pause()
            {
                if (!PLAYING_SOUND)
                    return;

                WAVE_OUT_EVENT.Pause();
                PLAYING_SOUND = false;
            }

            public List<Sound> GetList() => LIST.ToList();
            public int GetSelectionIndex() => INDEX_SELECTION;
            public int GetPlayingIndex() => INDEX_PLAYING;
            public int SoundCount
            {
                get { return LIST.Count; }
            }
            public Grid GetGrid() => GRID;

            void Refresh()
            {
                int index_start = 0;
                if(INDEX_SELECTION > MAX_PER_WINDOW)
                {
                    int window_num = (INDEX_SELECTION / MAX_PER_WINDOW) ; //30 / 28 = 1
                    index_start = MAX_PER_WINDOW * window_num; //
                }
                GRID = new Grid();
                GRID.AddColumn();

                

                for(int i = index_start; i < LIST.Count; i++)
                {
                    Sound sound = LIST.ElementAt(i);
                    string font;

                    if (INDEX_SELECTION == i)
                        font = FONT_SELECTED;
                    else if (INDEX_PLAYING == i)
                        font = FONT_PLAYING;
                    else
                        font = FONT_NORMAL;

                    string name = FormatSoundName(sound.GetName());
                    
                    GRID.AddRow(new string[] { $"[{font}]{name}[/]" });

                    if (i - index_start > MAX_PER_WINDOW) //break if reached end of window
                        break;
                }
            }

            
            public void MoveSelectionIndex(int increment = 1)
            {
                INDEX_SELECTION += increment;

                if (INDEX_SELECTION < 0)
                    INDEX_SELECTION = LIST.Count - 1;
                else if (INDEX_SELECTION > LIST.Count - 1)
                    INDEX_SELECTION = 0;
                Console.WriteLine(INDEX_SELECTION);
                Refresh();
            }
        }


    }
}
