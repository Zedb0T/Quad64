using Collada141;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System.Drawing;
using System.Drawing.Imaging;

namespace Quad64
{
    class DumpModel
    {
        private static double _scale = 1.0;

        private static param MakeParam(string name, string type)
        {
            param p = new param();
            p.name = name;
            p.type = type;
            return p;
        }

        public static accessor MakePosAccessor(ulong count, string source)
        {
            accessor acc = new accessor();
            acc.stride = 3;
            acc.count = count;
            acc.source = "#" + source;
            acc.param = new param[] {
                MakeParam("X", "float"),
                MakeParam("Y", "float"),
                MakeParam("Z", "float")
            };
            return acc;
        }

        public static accessor MakeTexCoordAccessor(ulong count, string source)
        {
            accessor acc = new accessor();
            acc.stride = 2;
            acc.count = count;
            acc.source = "#" + source;
            acc.param = new param[] {
                MakeParam("S", "float"),
                MakeParam("T", "float")
            };
            return acc;
        }

        public static accessor MakeColorAccessor(ulong count, string source)
        {
            accessor acc = new accessor();
            acc.stride = 3;
            acc.count = count;
            acc.source = "#" + source;
            acc.param = new param[] {
                MakeParam("R", "float"),
                MakeParam("G", "float"),
                MakeParam("B", "float")
            };
            return acc;
        }

        public static source makePositionsSource(ref Model3D mdl)
        {
            source src = new source();
            src.id = "positions_source";
            float_array fa = new float_array();
            fa.id = "positions";

            List<double> values = new List<double>();
            for (int id_num = 0; id_num < mdl.meshes.Count; id_num++)
            {
                int len = mdl.meshes[id_num].vertices.Length;
                for (int i = 0; i < len; i++)
                {
                    values.Add(mdl.meshes[id_num].vertices[i].X * _scale);
                    values.Add(mdl.meshes[id_num].vertices[i].Y * _scale);
                    values.Add(mdl.meshes[id_num].vertices[i].Z * _scale);
                }
            }
            fa.Values = values.ToArray();
            fa.count = (ulong)fa.Values.LongLength;

            src.technique_common = new sourceTechnique_common();
            src.technique_common.accessor = MakePosAccessor(fa.count / 3, fa.id);
            src.Item = fa;
            return src;
        }

        public static source makeTexCoordSource(ref Model3D mdl)
        {

            source src = new source();
            src.id = "texCoord_source";
            float_array fa = new float_array();
            fa.id = "texCoord";

            List<double> values = new List<double>();
            for (int id_num = 0; id_num < mdl.meshes.Count; id_num++)
            {
                bool clampX = mdl.meshes[id_num].texture.TextureParamS == 33071;
                bool clampY = mdl.meshes[id_num].texture.TextureParamT == 33071;
                int len = mdl.meshes[id_num].colors.Length;
                for (int i = 0; i < len; i++)
                {
                    float X = mdl.meshes[id_num].texCoord[i].X;
                    float Y = mdl.meshes[id_num].texCoord[i].Y;
                    if (clampX)
                        X = (X > 1.0f ? 1.0f : (X < 0.0f ? 0.0f : X));
                    if (clampY)
                        Y = (Y > 1.0f ? 1.0f : (Y < 0.0f ? 0.0f : Y));

                    values.Add(X);
                    values.Add(-Y);
                }
            }
            fa.Values = values.ToArray();
            fa.count = (ulong)fa.Values.LongLength;
            
            src.technique_common = new sourceTechnique_common();
            src.technique_common.accessor = MakeTexCoordAccessor(fa.count / 2, fa.id);
            src.Item = fa;
            return src;
        }

        public static source makeColorSource(ref Model3D mdl)
        {
            source src = new source();
            src.id = "color_source";
            float_array fa = new float_array();
            fa.id = "color";

            List<double> values = new List<double>();
            for (int id_num = 0; id_num < mdl.meshes.Count; id_num++)
            {
                int len = mdl.meshes[id_num].colors.Length;
                for (int i = 0; i < len; i++)
                {
                    values.Add(mdl.meshes[id_num].colors[i].X);
                    values.Add(mdl.meshes[id_num].colors[i].Y);
                    values.Add(mdl.meshes[id_num].colors[i].Z);
                }
            }
            fa.Values = values.ToArray();
            fa.count = (ulong)fa.Values.LongLength;

            src.technique_common = new sourceTechnique_common();
            src.technique_common.accessor = MakeColorAccessor(fa.count / 3, fa.id);
            src.Item = fa;
            return src;
        }

        public static InputLocal makeInput(string source, string semantic)
        {
            InputLocal input = new InputLocal();
            input.source = "#" + source;
            input.semantic = semantic;
            return input;
        }

        public static InputLocalOffset makeInputOffset(string source, string semantic, 
            ulong offset, bool setSpecified, ulong set)
        {
            InputLocalOffset input = new InputLocalOffset();
            input.source = "#" + source;
            input.semantic = semantic;
            input.offset = offset;
            input.set = set;
            input.setSpecified = setSpecified;
            return input;
        }

        public static vertices MakeVertices()
        {
            vertices verts = new vertices();
            verts.id = "vertices";
            verts.input = new InputLocal[] { makeInput("positions_source", "POSITION") };
            return verts;
        }

        public static polylist MakePolyList(ulong id_num, uint[] indices, ref uint largest_offset)
        {
            polylist plist = new polylist();
            plist.material = "MaterialInstance_" + id_num;
            plist.input = new InputLocalOffset[] {
                makeInputOffset("vertices", "VERTEX", 0, false, 0),
                makeInputOffset("texCoord_source", "TEXCOORD", 1, true, 0),
                makeInputOffset("color_source", "COLOR", 2, true, 0)
            };
            string p = "", vcount = "";
            long len = indices.LongLength;
            uint largest = largest_offset;
            for (long i = 0; i < len; i++)
            {
                if(i % 3 == 0)
                    vcount += "3 ";
                uint index = indices[i] + largest_offset;
                largest = Math.Max(largest, indices[i] + largest_offset);
                p += index.ToString() + " " + index.ToString() + " " + index.ToString();
                if (i < len - 1)
                    p += " ";
            }
            largest_offset = largest + 1;

            plist.vcount = vcount;
            plist.count = (ulong)(indices.LongLength / 3);
            plist.p = p;
            
            return plist;
        }

public static library_geometries MakeGeometryLibrary(ref Model3D mdl)
{
    library_geometries _geometries = new library_geometries();
    ulong count = (ulong) mdl.builder.TextureImages.Count;
    geometry geometry = new geometry();
    geometry = new geometry {
        id = "geometry"
    };
    mesh mesh = new mesh();
    mesh.source = new source[] { makePositionsSource(ref mdl), makeTexCoordSource(ref mdl), makeColorSource(ref mdl) };
    mesh.vertices = MakeVertices();
    List<object> list = new List<object>();
    uint num2 = 0;
    ulong num3 = 0UL;
    while (true)
    {
        if (num3 >= count)
        {
            mesh.Items = list.ToArray();
            geometry.Item = mesh;
            _geometries.geometry = new geometry[] { geometry };
            return _geometries;
        }
        list.Add(MakePolyList(num3, mdl.meshes[(int) num3].indices, ref num2));
        num3 += (ulong) 1L;
    }
}



        private static image MakeImage(ulong id_num, string modelName)
        {
            image img = new image();
            img.id = "image_"+id_num;
            img.Item = modelName + "/" + id_num + ".png";
            return img;
        }

private static library_images MakeImagesLibrary(ref Model3D mdl, string folder)
{
    library_images _images = new library_images();
    image[] imageArray = new image[mdl.builder.TextureImages.Count];
    ulong num2 = 0UL;
    while (true)
    {
        if (num2 >= (ulong) imageArray.Length)
        {
            _images.image = imageArray;
            return _images;
        }
        imageArray[(int) ((IntPtr) num2)] = MakeImage(num2, folder);
        num2 += (ulong) 1L;
    }
}

 

        
        private static common_newparam_type MakeNewParamForEffect(ulong id_num, bool isSampler2D)
        {
            common_newparam_type newparam = new common_newparam_type();
            if (isSampler2D)
            {
                fx_sampler2D_common sampler2D = new fx_sampler2D_common();
                sampler2D.source = "surface_" + id_num;
                newparam.Item = sampler2D;
                newparam.ItemElementName = ItemChoiceType.sampler2D;
                newparam.sid = "sampler2D_" + id_num;
            }
            else
            {
                fx_surface_init_from_common initFrom = new fx_surface_init_from_common();
                fx_surface_common surface = new fx_surface_common();
                initFrom.Value = "image_" + id_num;
                surface.type = fx_surface_type_enum.Item2D;
                surface.init_from = new fx_surface_init_from_common[] {
                    initFrom
                };
                newparam.Item = surface;
                newparam.ItemElementName = ItemChoiceType.surface;
                newparam.sid = "surface_" + id_num;
            }
            return newparam;
        }

        private static effectFx_profile_abstractProfile_COMMON MakeProfileCOMMON(ulong id_num)
        {
            effectFx_profile_abstractProfile_COMMON proCom = new effectFx_profile_abstractProfile_COMMON();
            proCom.technique = new effectFx_profile_abstractProfile_COMMONTechnique();
            effectFx_profile_abstractProfile_COMMONTechniqueLambert lambert = new effectFx_profile_abstractProfile_COMMONTechniqueLambert();
            common_color_or_texture_type diffuse = new common_color_or_texture_type();
            common_color_or_texture_typeTexture tex = new common_color_or_texture_typeTexture();
            tex.texture = "sampler2D_"+ id_num;
            diffuse.Item = tex;
            lambert.diffuse = diffuse;
            proCom.Items = new object[] {
                MakeNewParamForEffect(id_num, false),
                MakeNewParamForEffect(id_num, true)
            };
            proCom.technique.Item = lambert;
            return proCom;
        }

        private static effect MakeEffect(ulong id_num)
        {
            effect eff = new effect();
            eff.id = "effect_" + id_num;
            eff.Items = new effectFx_profile_abstractProfile_COMMON[]
            {
                MakeProfileCOMMON(id_num)
            };
            return eff;
        }

private static library_effects MakeEffectsLibrary(ref Model3D mdl, string folder)
{
    library_effects _effects = new library_effects();
    effect[] effectArray = new effect[mdl.builder.TextureImages.Count];
    ulong num2 = 0UL;
    while (true)
    {
        if (num2 >= (ulong) effectArray.Length)
        {
            _effects.effect = effectArray;
            return _effects;
        }
        effectArray[(int) ((IntPtr) num2)] = MakeEffect(num2);
        num2 += (ulong) 1L;
    }
}

 

private static material MakeMaterial(ulong id_num)
{
    material material = new material {
        name = "Material_" + id_num.ToString(),
        id = "material_" + id_num.ToString()
    };
    instance_effect _effect = new instance_effect {
        url = "#effect_" + id_num.ToString()
    };
    material.instance_effect = _effect;
    return material;
}

 


private static library_materials MakeMaterialsLibrary(ref Model3D mdl)
{
    library_materials _materials = new library_materials();
    material[] materialArray = new material[mdl.builder.TextureImages.Count];
    ulong num2 = 0UL;
    while (true)
    {
        if (num2 >= (ulong) materialArray.Length)
        {
            _materials.material = materialArray;
            return _materials;
        }
        materialArray[(int) ((IntPtr) num2)] = MakeMaterial(num2);
        num2 += (ulong) 1L;
    }
}

 

        private static bind_material MakeBindedMaterial(ref Model3D mdl)
        {
            bind_material bm = new bind_material();
            instance_material[] materials = new instance_material[mdl.builder.TextureImages.Count];
            for (int id_num = 0; id_num < materials.Length; id_num++)
            {
                materials[id_num] = new instance_material();
                materials[id_num].symbol = "MaterialInstance_" + id_num;
                materials[id_num].target = "#material_" + id_num;
            }

            bm.technique_common = materials;
            return bm;
        }

        private static library_visual_scenes MakeVisualScenesLibrary(ref Model3D mdl, ref Matrix4 mat)
{
    library_visual_scenes _scenes = new library_visual_scenes();
    visual_scene _scene = new visual_scene {
        id = "scene"
    };
    node node = new node {
        id = "node",
        name = "node",
        type = NodeType.NODE
    };
    matrix matrix = new matrix();
    double[] numArray1 = new double[0x10];
    numArray1[0] = mat.M11;
    numArray1[1] = mat.M21;
    numArray1[2] = mat.M31;
    numArray1[3] = mat.M41;
    numArray1[4] = mat.M12;
    numArray1[5] = mat.M22;
    numArray1[6] = mat.M32;
    numArray1[7] = mat.M42;
    numArray1[8] = mat.M13;
    numArray1[9] = mat.M23;
    numArray1[10] = mat.M33;
    numArray1[11] = mat.M43;
    numArray1[12] = mat.M14;
    numArray1[13] = mat.M24;
    numArray1[14] = mat.M34;
    numArray1[15] = mat.M44;
    matrix.Values = numArray1;
    node.Items = new object[] { matrix };
    node.ItemsElementName = new ItemsChoiceType2[] { ItemsChoiceType2.matrix };
    matrix.sid = "transform";
    int count = mdl.builder.TextureImages.Count;
    instance_geometry _geometry = new instance_geometry {
        url = "#geometry",
        bind_material = MakeBindedMaterial(ref mdl)
    };
    node.instance_geometry = new instance_geometry[] { _geometry };
    _scene.node = new node[] { node };
    _scenes.visual_scene = new visual_scene[] { _scene };
    return _scenes;
}



        private static void WriteAllTextures(List<Bitmap> textures, string folder)
{
    for (int i = 0; i < textures.Count; i++)
    {
        string fileName = Path.Combine(folder, $"{i}.png");
        new FileInfo(fileName).Directory.Create();
        textures[i].Save(fileName, ImageFormat.Png);
    }
}

 


        public static void dumpModelToCOLLADA(Model3D mdl, Object3D obj, float scale, string fileName)
{
    _scale = scale;
    COLLADA collada = new COLLADA {
        scene = new COLLADAScene()
    };
    collada.scene.instance_visual_scene = new InstanceWithExtra();
    collada.scene.instance_visual_scene.url = "#scene";
    collada.asset = new asset();
    collada.asset.unit = new assetUnit();
    collada.asset.unit.meter = 1.0;
    collada.asset.unit.name = "meter";
    collada.asset.up_axis = UpAxisType.Y_UP;
    Matrix4 identity = Matrix4.Identity;
    if (obj != null)
    {
        identity = Matrix4.CreateTranslation((float) obj.xPos, (float) obj.yPos, (float) obj.zPos);
    }
    fileName = fileName.Replace(" ", string.Empty);
    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Path.GetDirectoryName(fileName), Path.GetFileNameWithoutExtension(fileName) + "_tex");
    if (!Directory.Exists(path))
    {
        Directory.CreateDirectory(path);
    }
    collada.Items = new object[] { MakeImagesLibrary(ref mdl, path), MakeEffectsLibrary(ref mdl, path), MakeMaterialsLibrary(ref mdl), MakeGeometryLibrary(ref mdl), MakeVisualScenesLibrary(ref mdl,  ref identity) };
    WriteAllTextures(mdl.builder.TextureImages, path);
    collada.Save(fileName + ".dae");
}


    }
}
