using Spectre.Console;
using System.Data;
using System.Text.Json;
using static MusicPlayerConsole.Global;
using Rule = Spectre.Console.Rule;

namespace MusicPlayerConsole
{
    
    internal class Program
    {
       
        static void Main(string[] args)
        {
            new Program().OnStart();
        }


        public void OnStart()
        {
            var (playlist, controls) = LoadConfigSettings();
            string layout_left_id = "Playlist", layout_right_id = "Right", layout_right_row1_id = "Playing", layout_right_row2_id = "Next",
                layout_right_row3_id = "Controls";
            Layout layout = new Layout("Root")
                .SplitColumns(
                new Layout(layout_left_id),
                new Layout(layout_right_id)
                .SplitRows(
                    new Layout(layout_right_row1_id),
                    new Layout(layout_right_row2_id),
                    new Layout(layout_right_row3_id)
                    ));

            var controls_layout_string = string.Join("\n", new string[] {
                "Go to previous: "+controls.PreviousSound,
                "Go to next: "+controls.NextSound,
                "Pause: "+controls.PauseSound,
                "Play: "+controls.PlaySound
            });
            layout[layout_right_id]["Controls"].Update(new Panel(
                controls_layout_string
                ));
            
            layout[layout_left_id].Size(80);
            AnsiConsole.Live(layout)
                .Start(ctx =>
                {
                    void PreviousSound()
                    {
                        int index_prev_sound = playlist.GetPlayingIndex() - 1; // 4
                        if (index_prev_sound < 0)
                            index_prev_sound = playlist.SoundCount - 1;
                        int index_selection = playlist.GetSelectionIndex(); // 7
                        int difference = index_prev_sound - index_selection; // -3

                        playlist.MoveSelectionIndex(difference);
                        playlist.Play(true);
                        UpdatePlaying();
                    }
                    void NextSound()
                    {
                        int index_next_sound = playlist.GetPlayingIndex() + 1;
                        if (index_next_sound == playlist.SoundCount)
                            index_next_sound = 0;
                        int index_selection = playlist.GetSelectionIndex();
                        int difference = index_next_sound - index_selection;

                        playlist.MoveSelectionIndex(difference);
                        playlist.Play(true);
                        UpdatePlaying();
                    }
                    void PauseSound()
                    {
                        playlist.Pause();
                    }
                    void PlaySound()
                    {
                        if (playlist.GetSelectionIndex() != playlist.GetPlayingIndex())
                        {
                            playlist.Play(true);
                            UpdatePlaying();
                        }
                        else 
                            playlist.Play(false);
                        
                    }
                    void UpdatePlaying()
                    {
                        int current_sound_index = playlist.GetPlayingIndex();
                        int next_sound_index = current_sound_index + 1;
                        if (next_sound_index == playlist.SoundCount)
                            next_sound_index = 0;
                        //update current playing panel
                        layout[layout_right_id][layout_right_row1_id].Update(new Panel(
                            "Playing: " + playlist.GetList()[current_sound_index].GetName()
                            ));
                        //update next panel
                        layout[layout_right_id][layout_right_row2_id].Update(new Panel(
                            "Next: "+playlist.GetList()[next_sound_index].GetName()
                        ));
                    }
                    void Refresh()
                    {
                        
                        //update playlist
                        layout[layout_left_id].Update(
                        new Panel(
                            Align.Left(
                            playlist.GetGrid(),
                            VerticalAlignment.Middle))
                        .Expand());
                        AnsiConsole.Clear();
                        ctx.Refresh();
                    }

                    Refresh();
                    while (true)
                    {
                        ConsoleKey key = Console.ReadKey().Key;

                        if (key == controls.Up)
                            playlist.MoveSelectionIndex(-1);
                        else if (key == controls.Down)
                            playlist.MoveSelectionIndex();
                        else if (key == controls.PlaySound)
                            PlaySound();
                        else if (key == controls.PauseSound)
                            PauseSound();
                        else if (key == controls.NextSound)
                            NextSound();
                        else if (key == controls.PreviousSound)
                            PreviousSound();
                        else if (key == ConsoleKey.Escape)
                            Environment.Exit(0);
 

                        Refresh();
                    }
                });

            
        }
        

        static List<Sound> GetSoundsFromDirectory(string directory)
        {
            List<Sound> list = new List<Sound>();

            
            foreach (string entry_name in Directory.GetFileSystemEntries(directory))
            {
                //if entry is a file
                if (File.Exists(entry_name))
                {
                    //if file ext is in supported list
                    string entry_ext = Path.GetExtension(entry_name)[1..];

                    if (SUPPORTED_FILE_EXTENSIONS.Contains(entry_ext))
                    {
                        list.Add(Sound.FromFile(entry_name));
                    }
                }
                else if (Directory.Exists(entry_name)) //if entry is a directory
                    list.AddRange(GetSoundsFromDirectory(entry_name));
            }

            return list;
        }

        (Playlist, Controls) LoadConfigSettings()
        {
            Config config = LoadConfig();

            if (config.GetSoundPathsCount() == 0)
            {
                //get a path to load sound files from
                while(true)
                {
                    string sound_path = AnsiConsole.Ask<string>("Input a path to load song files:");

                    if (!Directory.Exists(sound_path))
                        AnsiConsole.MarkupLine("[red]Invalid directory.[/] Try again!");
                    else
                    {
                        config.AddSoundPath(sound_path);
                        
                        SaveConfig(config);
                        break;
                    }
                }
            }
            //get sound filenames
            List<Sound> sounds = new List<Sound>();
            int sound_path_count = config.GetSoundPaths().Count;
            AnsiConsole.Progress().Start(ctx =>
            {
                var task_load_sounds = ctx.AddTask($"Loading sounds from {sound_path_count} directories");
                float task_load_sounds_step = 100 / sound_path_count;

                foreach (string sound_path in config.GetSoundPaths())
                {
                    sounds.AddRange(GetSoundsFromDirectory(sound_path));
                    task_load_sounds.Increment(task_load_sounds_step);
                }
                
            });

            Playlist playlist = new Playlist(sounds, ("white on black", "black on white", "green on black"));
            Controls controls = config.controls;
            return (playlist, controls);
        }

        
    }
}