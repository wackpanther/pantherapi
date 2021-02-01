using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PantherAPI
{
    public class Counter
    {
        public Counter() { }

        public Counter(string Name, string Label, int Seed = 0) 
        {
            this.Name = Name;
            this.Label = Label;
            this.CurrentCount = Seed;
        }

        public int Id { get; set; }
        public int CurrentCount { get; set; }
        public string Name { get; set; }
        public string Label { get; set; }

        public int Add(int Amount = 1)
        {
            CurrentCount += Amount;
            return CurrentCount;
        }

        public int Subtract(int Amount = 1)
        {
            CurrentCount -= Amount;
            CurrentCount = Math.Max(0, CurrentCount);
            return CurrentCount;
        }

        public void Reset()
        {
            CurrentCount = 0;
        }
    }
}
