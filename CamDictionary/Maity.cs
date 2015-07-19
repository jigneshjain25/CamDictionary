using System;
using System.IO;
using SQLite;
using Windows.UI.Xaml.Controls;
using System.Diagnostics;

namespace CamDictionary
{
    [Table ("entries")]
    class Entry
    {
        [MaxLength(30), NotNull]
        public String Word { get; set; }
        [MaxLength(30)]
        public String WordType { get; set; }
        [NotNull]
        public String Definition { get; set; }
    }

    public class InternalDictionary
    {

       SQLiteAsyncConnection conn;
       
        public InternalDictionary()
        {
            string path = Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "LocalDictionary.db");            
            conn = new SQLiteAsyncConnection(path);            
        }

        public async void searchForWord(String Word, TextBlock textBlock)
        {
            textBlock.Text = "Loading . . . ";
            Word = Char.ToUpper(Word[0]) + Word.Substring(1).ToLower();
            Debug.WriteLine("Word : " + Word);
            
            await conn.CreateTableAsync<Entry>();
            var query = conn.Table<Entry>().Where(x => x.Word == Word);
            var result = await query.ToListAsync();

            Debug.WriteLine("Count : " + result.Count);

            if (result.Count == 0)
            {
                textBlock.Text = "Word not found";
            }
            else
            {
                foreach (var item in result)
                {
                    textBlock.Text = item.Definition;
                    System.Diagnostics.Debug.WriteLine("\nMeaning of the word  \"" + Word + "\" is : " + item.Definition);
                    break;
                }
            }            
        }
    }
}
