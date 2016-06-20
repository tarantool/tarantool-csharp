﻿namespace Tarantool.Client
{
    public class SpaceField
    {
        public SpaceField(string name, FieldType type)
        {
            Name = name;
            Type = type;
        }

        public string Name { get; } 

        public FieldType Type { get; }
    }
}