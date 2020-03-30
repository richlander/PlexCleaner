﻿namespace PlexCleaner
{
    public class ConvertOptions
    {
        public int VideoEncodeQuality { get; set; } = 20;
        public string AudioEncodeCodec { get; set; } = "ac3";
        public bool TestSnippets { get; set; } = false;
    }
}