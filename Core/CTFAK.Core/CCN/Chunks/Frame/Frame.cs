﻿using CTFAK.CCN.Chunks.Objects;
using CTFAK.Memory;
using CTFAK.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static CTFAK.CCN.Chunks.Objects.ObjectInfo;

namespace CTFAK.CCN.Chunks.Frame
{
    public class ObjectInstance : ChunkLoader
    {
        public ushort handle;
        public ushort objectInfo;
        public int x;
        public int y;
        public short parentType;
        public short layer;
        public short flags;
        public short parentHandle;
        public override void Read(ByteReader reader)
        {
            handle = (ushort)reader.ReadInt16();
            objectInfo = (ushort)reader.ReadInt16();

            if (Settings.Old)
            {
                y = reader.ReadInt16();
                x = reader.ReadInt16();
            }
            else
            {
                x = reader.ReadInt32();
                y = reader.ReadInt32();
            }
            parentType = reader.ReadInt16();
            parentHandle = reader.ReadInt16();
            if (Settings.Old) return;
            layer = reader.ReadInt16();
            flags = reader.ReadInt16();

        }

        public override void Write(ByteWriter writer)
        {
            throw new NotImplementedException();
        }
    }
    public class VirtualRect : ChunkLoader
    {
        public int left;
        public int top;
        public int right;
        public int bottom;
        public override void Read(ByteReader reader)
        {
            left = reader.ReadInt32();
            top = reader.ReadInt32();
            right = reader.ReadInt32();
            bottom = reader.ReadInt32();
        }

        public override void Write(ByteWriter writer)
        {
            throw new NotImplementedException();
        }
    }
    public class Frame : ChunkLoader
    {
        public string name;
        public int width;
        public int height;
        public Color background;
        public Events events;
        public BitDict flags = new BitDict(new string[]
        {
            "XCoefficient",
            "YCoefficient",
            "DoNotSaveBackground",
            "Wrap",
            "Visible",
            "WrapHorizontally",
            "WrapVertically","","","","","","","","","",
            "Redraw",
            "ToHide",
            "ToShow"

        });
        public List<ObjectInstance> objects = new();
        public Layers layers;
        public List<Color> palette;
        public Transition fadeIn;
        public Transition fadeOut;
        public VirtualRect virtualRect;
        public ShaderData shaderData = new();
        public int InkEffect;
        public int InkEffectValue;
        public Color rgbCoeff;
        public byte blend;

        public override void Read(ByteReader reader)
        {
            while (true)
            {
                var newChunk = new Chunk();
                var chunkData = newChunk.Read(reader);
                var chunkReader = new ByteReader(chunkData);
                //Logger.Log("Reading Frame Chunk: " + newChunk.Id);
                if (newChunk.Id == 32639) break;
                if (reader.Tell() >= reader.Size()) break;
                switch (newChunk.Id)
                {

                    case 13109:
                        var frameName = new StringChunk();
                        frameName.Read(chunkReader);
                        if (string.IsNullOrEmpty(frameName.value))
                            name = "CORRUPTED FRAME";
                        else
                            name = frameName.value;
                        break;
                    case 13108:
                        if (Settings.Old)
                        {
                            width = chunkReader.ReadInt16();
                            height = chunkReader.ReadInt16();
                            background = chunkReader.ReadColor();
                            flags.flag = chunkReader.ReadUInt16();
                        }
                        else
                        {
                            width = chunkReader.ReadInt32();
                            height = chunkReader.ReadInt32();
                            background = chunkReader.ReadColor();
                            flags.flag = chunkReader.ReadUInt32();
                        }
                        
                        break;
                    case 13112:
                        var count = chunkReader.ReadInt32();
                        for (int i = 0; i < count; i++)
                        {
                            var objInst = new ObjectInstance();
                            objInst.Read(chunkReader);
                            objects.Add(objInst);
                        }
                        
                        break;
                    case 13117:
                        if (CTFAKCore.parameters.Contains("-noevnt"))
                            events = new Events();
                        else
                        {
                            events = new Events();
                            events.Read(chunkReader);
                        }
                        break;
                    case 13121:
                        layers = new Layers();
                        layers.Read(chunkReader);
                        break;
                    case 13111:
                        var pal = new FramePalette();
                        pal.Read(chunkReader);
                        palette = pal.Items;
                        break;
                    case 13115:
                        fadeIn = new Transition();
                        fadeIn.Read(chunkReader);
                        break;
                    case 13116:
                        fadeOut = new Transition();
                        fadeOut.Read(chunkReader);
                        break;
                    case 13122:
                        virtualRect = new VirtualRect();
                        virtualRect.Read(chunkReader);
                        break;
                    case 13125: // Layer Effects
                        var start = chunkReader.Tell();
                        var end = start + chunkReader.Size();
                        if (start == end) break;

                        int current = 0;
                        while (true)
                        {
                            if (chunkReader.Tell() == end) break;
                            var layer = layers.Items[current];
                            layer.InkEffect = chunkReader.ReadInt32();
                            if (layer.InkEffect != 1)
                            {
                                var r = chunkReader.ReadByte();
                                var g = chunkReader.ReadByte();
                                var b = chunkReader.ReadByte();
                                layer.rgbCoeff = Color.FromArgb(0, r, g, b);
                                layer.blend = chunkReader.ReadByte();
                            }
                            else
                                layer.InkEffectValue = chunkReader.ReadByte();

                            layer.shaderData.hasShader = true;
                            try
                            {

                                var shaderHandle = chunkReader.ReadInt32();
                                var numberOfParams = chunkReader.ReadInt32();
                                var shdr = CTFAKCore.currentReader.getGameData().shaders.ShaderList[shaderHandle];
                                layer.shaderData.name = shdr.Name;
                                layer.shaderData.ShaderHandle = shaderHandle;

                                for (int i = 0; i < numberOfParams; i++)
                                {
                                    var param = shdr.Parameters[i];
                                    object paramValue;
                                    switch (param.Type)
                                    {
                                        case 0:
                                            paramValue = chunkReader.ReadInt32();
                                            break;
                                        case 1:
                                            paramValue = chunkReader.ReadSingle();
                                            break;
                                        case 2:
                                            paramValue = chunkReader.ReadInt32();
                                            break;
                                        case 3:
                                            paramValue = chunkReader.ReadInt32(); //Image Handle
                                            break;
                                        default:
                                            paramValue = "unknownType";
                                            break;
                                    }
                                    layer.shaderData.parameters.Add(new ObjectInfo.ShaderParameter()
                                    {
                                        Name = param.Name,
                                        ValueType = param.Type,
                                        Value = paramValue
                                    });
                                }
                                //Logger.Log($"Shader Handle: {shaderHandle}\nShader Name: {shdr.Name}\nNumber of Params: {numberOfParams}");
                            }
                            catch // No Shader Found
                            {
                                layer.shaderData.hasShader = false;
                                current++;
                                break;
                            }
                            chunkReader.Seek(chunkReader.Tell() + 4);
                            current++;
                        }
                        break;
                    case 13129: // Frame Effects
                        InkEffect = chunkReader.ReadInt32();
                        if (InkEffect != 1)
                        {
                            var r = chunkReader.ReadByte();
                            var g = chunkReader.ReadByte();
                            var b = chunkReader.ReadByte();
                            rgbCoeff = Color.FromArgb(0, r, g, b);
                            blend = chunkReader.ReadByte();
                        }
                        else
                            InkEffectValue = chunkReader.ReadByte();

                        shaderData.hasShader = false;
                        try
                        {
                            var shaderHandle = chunkReader.ReadInt32();
                            if (shaderHandle == 255) break;
                            var numberOfParams = chunkReader.ReadInt32();
                            var shdr = CTFAKCore.currentReader.getGameData().shaders.ShaderList[shaderHandle];
                            shaderData.name = shdr.Name;
                            shaderData.ShaderHandle = shaderHandle;

                            for (int i = 0; i < numberOfParams; i++)
                            {
                                var param = shdr.Parameters[i];
                                object paramValue;
                                switch (param.Type)
                                {
                                    case 0:
                                        paramValue = chunkReader.ReadInt32();
                                        break;
                                    case 1:
                                        paramValue = chunkReader.ReadSingle();
                                        break;
                                    case 2:
                                        paramValue = chunkReader.ReadInt32();
                                        break;
                                    case 3:
                                        paramValue = chunkReader.ReadInt32(); //Image Handle
                                        break;
                                    default:
                                        paramValue = "unknownType";
                                        break;
                                }
                                shaderData.parameters.Add(new ShaderParameter()
                                {
                                    Name = param.Name,
                                    ValueType = param.Type,
                                    Value = paramValue
                                });
                            }
                            //Logger.Log($"Shader Handle: {shaderHandle}\nShader Name: {shdr.Name}\nNumber of Params: {numberOfParams}");
                        }
                        catch // No Shader Found
                        {
                            shaderData.hasShader = false;
                        }
                        break;
                }
            }

            Logger.Log($"Frame Found: {name}, {width}x{height}, {objects.Count} objects.", true, ConsoleColor.Green);
        }
        

        public override void Write(ByteWriter writer)
        {
            throw new NotImplementedException();
        }
    }
    public class Layers : ChunkLoader
    {
        public List<Layer> Items;


        public override void Read(ByteReader reader)
        {
            Items = new List<Layer>();
            var count = reader.ReadUInt32();
            for (int i = 0; i < count; i++)
            {
                Layer item = new Layer();
                item.Read(reader);
                Items.Add(item);
            }

        }

        public override void Write(ByteWriter Writer)
        {
            Writer.WriteInt32(Items.Count);
            foreach (Layer layer in Items)
            {
                layer.Write(Writer);
            }
        }



    }

    public class Layer : ChunkLoader
    {
        public string Name;
        public BitDict Flags = new BitDict(new string[]
        {
            "XCoefficient",
            "YCoefficient",
            "DoNotSaveBackground",
            "",
            "Visible",
            "WrapHorizontally",
            "WrapVertically",
            "", "", "", "",
            "", "", "", "", "",
            "Redraw",
            "ToHide",
            "ToShow"
        }

        );
        public float XCoeff;
        public float YCoeff;
        public int NumberOfBackgrounds;
        public int BackgroudIndex;
        public ShaderData shaderData = new();
        public int InkEffect;
        public int InkEffectValue;
        public Color rgbCoeff;
        public byte blend;

        public override void Read(ByteReader reader)
        {
            Flags.flag = reader.ReadUInt32();
            XCoeff = reader.ReadSingle();
            YCoeff = reader.ReadSingle();
            NumberOfBackgrounds = reader.ReadInt32();
            BackgroudIndex = reader.ReadInt32();
            Name = reader.ReadUniversal();
            if (Settings.android)
            {
                XCoeff = 1;
                YCoeff = 1;
            }
        }

        public override void Write(ByteWriter Writer)
        {
            Writer.WriteInt32((int)Flags.flag);
            Writer.WriteSingle(XCoeff);
            Writer.WriteSingle(YCoeff);
            Writer.WriteInt32(NumberOfBackgrounds);
            Writer.WriteInt32(BackgroudIndex);
            Writer.WriteUnicode(Name);
        }



    }

    public class FramePalette : ChunkLoader
    {
        public List<Color> Items;

        public override void Read(ByteReader reader)
        {
            Items = new List<Color>();
            for (int i = 0; i < 257; i++)
            {
                Items.Add(reader.ReadColor());
            }
        }

        public override void Write(ByteWriter Writer)
        {
            foreach (Color item in Items)
            {
                Writer.WriteColor(item);
            }
        }

    }

}
