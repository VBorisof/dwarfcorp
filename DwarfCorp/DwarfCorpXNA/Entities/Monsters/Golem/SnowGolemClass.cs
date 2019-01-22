// Elf.cs
// 
//  Modified MIT License (MIT)
//  
//  Copyright (c) 2015 Completely Fair Games Ltd.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// The following content pieces are considered PROPRIETARY and may not be used
// in any derivative works, commercial or non commercial, without explicit 
// written permission from Completely Fair Games:
// 
// * Images (sprites, textures, etc.)
// * 3D Models
// * Sound Effects
// * Music
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class SnowGolemClass : EmployeeClass
    {
        public SnowGolemClass()
        {
            if (!staticClassInitialized)
            {
                InitializeClassStatics();
            }
            if (!staticsInitiailized)
            {
                InitializeStatics();
            }
        }

        void InitializeLevels()
        {
            Levels = new List<Level>
            {
                new Level
                {
                    Index = 0,
                    Name = "Snow Golem",
                    Pay = 25,
                    XP = 0,
                    BaseStats = new CreatureStats.StatNums()
                    {
                        Constitution = 20.0f
                    }
                }
            };
        }

        void InitializeActions()
        {
            Actions =
                Task.TaskCategory.Gather |
                Task.TaskCategory.Guard |
                Task.TaskCategory.Attack;
        }

        void InitializeAnimations()
        {
            Animations = AnimationLibrary.LoadCompositeAnimationSet(ContentPaths.Entities.snowman_animation, "Snowman");
        }

        public void InitializeWeapons()
        {
            Attacks = new List<Attack>()
            {
                new Attack("Snowball", 0.1f, 1.0f, 50.0f, ContentPaths.Audio.demon_attack, ContentPaths.Effects.hit)
                {
                    Mode = Attack.AttackMode.Ranged,
                    LaunchSpeed = 10.0f,
                    ProjectileType = "Snowball",
                    TriggerMode = Attack.AttackTrigger.Timer
                }
            };
        }

        protected override sealed void InitializeStatics()
        {
            Name = "Snow Golem";
            InitializeLevels();
            InitializeAnimations();
            InitializeWeapons();
            InitializeActions();
            base.InitializeStatics();
        }
    }
}
