﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TodoList
{
    [Command(
        Name: "complete",
        ShortDescription: "Marks a todo task as complete.",
        ErrorText: "",
        LongHelpText: "Change the status of a todo task to complete. This will hide the task from the list command unless -all is passed to list. It also causes the task to display in blue."
    )]
    internal class Complete : ICommand
    {
        [DefaultSwitch(0)] public UInt32 id = 0;

        public string file = "todo.txt";

        public void Invoke()
        {
            if (String.IsNullOrEmpty(file))
            {
                Console.WriteLine("No file specified. How did you manage that? It defaults to todo.txt");
                return;
            }

            if (id == 0)
            {
                Console.WriteLine("You need to specify the entry you're editing.");
                return;
            }

            var list = EntryList.LoadFile(file, true);

            var entry = list.Root.FindChildWithID(id);
            if (entry == null)
            {
                Console.WriteLine("Could not find entry with ID{0}.", id);
                return;
            }

            entry.Status = "✓";
            EntryList.SaveFile(file, list);
            Presentation.OutputEntry(entry, null, 0, true);
        }
    }
}