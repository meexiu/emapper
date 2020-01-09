using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace emapper
{
    public static class emapper
    {
        private static Hashtable _ht = Hashtable.Synchronized(new Hashtable());
        private static readonly object _ls = new object();

        public static t_out map<t_out>(object input)
        {
            if (input == null)
            {
                return default(t_out);
            }

            return get<t_out>(input.GetType()).map(input);
        }

        public static List<t_out> maps<t_out>(IEnumerable inputs)
        {
            if (inputs == null)
            {
                return null;
            }

            return get<t_out>(inputs.GetType().GenericTypeArguments[0]).maps(inputs);
        }

        private static emapper_instance<t_out> get<t_out>(Type input)
        {
            var key = $"{input}&{typeof(t_out)}";
            // ReSharper disable once InconsistentlySynchronizedField
            var mapper = _ht[key] as emapper_instance<t_out>;
            if (mapper != null)
            {
                return mapper;
            }

            lock (_ls)
            {
                mapper = _ht[key] as emapper_instance<t_out>;
                if (mapper != null)
                {
                    return mapper;
                }

                mapper = new emapper_instance<t_out>(input);
                _ht[key] = mapper;
            }

            return mapper;
        }

        public class emapper_instance<t_out>
        {
            private static int _mindex = 1;
            private static MethodInfo _mmap = typeof(emapper).GetMethod("map", new[] { typeof(object) });
            private static MethodInfo _mmaps = typeof(emapper).GetMethod("maps", new[] { typeof(List<object>) });
            private static MethodInfo _mtolist = typeof(Enumerable).GetMethod("ToList");
            private static MethodInfo _mtoarray = typeof(Enumerable).GetMethod("ToArray");
            private static MethodInfo _marraycopy = typeof(Array).GetMethod("Copy", new[] { typeof(Array), typeof(Array), typeof(int) });
            private readonly emit_load _handler;
            public delegate t_out emit_load(object input);

            public emapper_instance(Type in_type)
            {
                var mname = "_dc" + System.Threading.Interlocked.Increment(ref _mindex);
                var method = new DynamicMethod(mname, typeof(t_out), new[] { typeof(object) }, typeof(t_out), true);
                var il = method.GetILGenerator();

                var out_type = typeof(t_out);
                var out_builder = il.DeclareLocal(out_type);
                var out_cinfo = out_type.GetConstructor(Type.EmptyTypes);
                var out_pinfos = out_type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(pi => pi.MemberType == MemberTypes.Property || pi.MemberType == MemberTypes.Field)
                    .Where(pi => pi.GetSetMethod() != null)
                    .ToList();

                var in_pinfos = in_type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(pi => pi.MemberType == MemberTypes.Property || pi.MemberType == MemberTypes.Field)
                    .Where(pi => pi.GetGetMethod() != null)
                    .ToList();

                if (out_cinfo != null)
                {
                    il.Emit(OpCodes.Newobj, out_cinfo);
                    il.Emit(OpCodes.Stloc, out_builder);

                    foreach (PropertyInfo out_pinfo in out_pinfos)
                    {
                        var in_pinfo = in_pinfos.FirstOrDefault(x => x.Name == out_pinfo.Name);

                        if (in_pinfo == null)
                        {
                            continue;
                        }

                        var in_ptype = in_pinfo.PropertyType;
                        var out_ptype = out_pinfo.PropertyType;

                        if (is_value(in_ptype))
                        {
                            if (in_ptype == out_ptype)
                            {
                                il.Emit(OpCodes.Ldloc, out_builder);
                                il.Emit(OpCodes.Ldarg_0);
                                il.Emit(OpCodes.Callvirt, in_pinfo.GetGetMethod());
                                il.Emit(OpCodes.Callvirt, out_pinfo.GetSetMethod());
                                il.Emit(OpCodes.Nop);
                            }
                        }
                        else if (is_list(in_ptype) && is_list(out_ptype))
                        {
                            var in_item_type = item_type(in_ptype);
                            var out_item_type = item_type(out_ptype);

                            if (in_item_type == null || out_item_type == null)
                            {
                                continue;
                            }

                            if (is_value(in_item_type))
                            {
                                if (in_ptype == out_ptype)
                                {
                                    var mcount = in_ptype.GetMethod("get_Count");
                                    var mgetrange = in_ptype.GetMethod("GetRange", new[] { typeof(int), typeof(int) });
                                    if (mcount == null || mgetrange == null)
                                    {
                                        continue;
                                    }

                                    var brtrue_label = il.DefineLabel();
                                    il.Emit(OpCodes.Ldarg_0);
                                    il.Emit(OpCodes.Callvirt, in_pinfo.GetGetMethod());
                                    il.Emit(OpCodes.Ldnull);
                                    il.Emit(OpCodes.Ceq);
                                    il.Emit(OpCodes.Brtrue, brtrue_label);
                                    il.Emit(OpCodes.Nop);
                                    il.Emit(OpCodes.Ldloc, out_builder);
                                    il.Emit(OpCodes.Ldarg_0);
                                    il.Emit(OpCodes.Callvirt, in_pinfo.GetGetMethod());
                                    il.Emit(OpCodes.Ldc_I4, 0);
                                    il.Emit(OpCodes.Ldarg_0);
                                    il.Emit(OpCodes.Callvirt, in_pinfo.GetGetMethod());
                                    il.Emit(OpCodes.Callvirt, mcount);
                                    il.Emit(OpCodes.Callvirt, mgetrange);
                                    il.Emit(OpCodes.Callvirt, out_pinfo.GetSetMethod());
                                    il.Emit(OpCodes.Nop);

                                    il.MarkLabel(brtrue_label);
                                }
                            }
                            else if (is_class(in_item_type) && is_class(out_item_type))
                            {
                                var brtrue_label = il.DefineLabel();
                                il.Emit(OpCodes.Ldarg_0);
                                il.Emit(OpCodes.Callvirt, in_pinfo.GetGetMethod());
                                il.Emit(OpCodes.Ldnull);
                                il.Emit(OpCodes.Ceq);
                                il.Emit(OpCodes.Brtrue, brtrue_label);
                                il.Emit(OpCodes.Nop);
                                il.Emit(OpCodes.Ldloc, out_builder);
                                il.Emit(OpCodes.Ldarg_0);
                                il.Emit(OpCodes.Callvirt, in_pinfo.GetGetMethod());
                                il.Emit(OpCodes.Call, _mmaps.MakeGenericMethod(out_item_type));
                                il.Emit(OpCodes.Callvirt, out_pinfo.GetSetMethod());
                                il.Emit(OpCodes.Nop);
                                il.MarkLabel(brtrue_label);
                            }
                        }
                        else if (is_array(in_ptype) && is_array(out_ptype))
                        {
                            var in_item_type = item_type(in_ptype);
                            var out_item_type = item_type(out_ptype);

                            if (in_item_type == null || out_item_type == null)
                            {
                                continue;
                            }

                            if (is_value(in_item_type))
                            {
                                if (in_ptype == out_ptype)
                                {
                                    var brtrue_label = il.DefineLabel();
                                    il.Emit(OpCodes.Ldarg_0);
                                    il.Emit(OpCodes.Callvirt, in_pinfo.GetGetMethod());
                                    il.Emit(OpCodes.Ldnull);
                                    il.Emit(OpCodes.Ceq);
                                    il.Emit(OpCodes.Brtrue, brtrue_label);
                                    il.Emit(OpCodes.Nop);
                                    il.Emit(OpCodes.Ldloc, out_builder);
                                    il.Emit(OpCodes.Ldarg_0);
                                    il.Emit(OpCodes.Callvirt, in_pinfo.GetGetMethod());
                                    il.Emit(OpCodes.Ldlen);
                                    il.Emit(OpCodes.Conv_I4);
                                    il.Emit(OpCodes.Newarr, out_item_type);
                                    il.Emit(OpCodes.Callvirt, out_pinfo.GetSetMethod());
                                    il.Emit(OpCodes.Nop);
                                    il.Emit(OpCodes.Ldarg_0);
                                    il.Emit(OpCodes.Callvirt, in_pinfo.GetGetMethod());
                                    il.Emit(OpCodes.Ldloc, out_builder);
                                    il.Emit(OpCodes.Callvirt, out_pinfo.GetGetMethod());
                                    il.Emit(OpCodes.Ldarg_0);
                                    il.Emit(OpCodes.Callvirt, in_pinfo.GetGetMethod());
                                    il.Emit(OpCodes.Ldlen);
                                    il.Emit(OpCodes.Conv_I4);
                                    il.Emit(OpCodes.Call, _marraycopy);
                                    il.Emit(OpCodes.Nop);
                                    il.MarkLabel(brtrue_label);
                                }
                            }
                            else if (is_class(in_item_type) && is_class(out_item_type))
                            {
                                var brtrue_label = il.DefineLabel();
                                il.Emit(OpCodes.Ldarg_0);
                                il.Emit(OpCodes.Callvirt, in_pinfo.GetGetMethod());
                                il.Emit(OpCodes.Ldnull);
                                il.Emit(OpCodes.Ceq);
                                il.Emit(OpCodes.Brtrue, brtrue_label);
                                il.Emit(OpCodes.Nop);
                                il.Emit(OpCodes.Ldloc, out_builder);
                                il.Emit(OpCodes.Ldarg_0);
                                il.Emit(OpCodes.Callvirt, in_pinfo.GetGetMethod());
                                il.Emit(OpCodes.Call, _mtolist.MakeGenericMethod(in_item_type));
                                il.Emit(OpCodes.Call, _mmaps.MakeGenericMethod(out_item_type));
                                il.Emit(OpCodes.Call, _mtoarray.MakeGenericMethod(out_item_type));
                                il.Emit(OpCodes.Callvirt, out_pinfo.GetSetMethod());
                                il.Emit(OpCodes.Nop);
                                il.MarkLabel(brtrue_label);
                            }
                        }
                        else if (is_class(in_ptype) && is_class(out_ptype))
                        {
                            var brtrue_label = il.DefineLabel();
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Callvirt, in_pinfo.GetGetMethod());
                            il.Emit(OpCodes.Ldnull);
                            il.Emit(OpCodes.Ceq);
                            il.Emit(OpCodes.Brtrue, brtrue_label);
                            il.Emit(OpCodes.Nop);
                            il.Emit(OpCodes.Ldloc, out_builder);
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Callvirt, in_pinfo.GetGetMethod());
                            il.Emit(OpCodes.Call, _mmap.MakeGenericMethod(out_ptype));
                            il.Emit(OpCodes.Callvirt, out_pinfo.GetSetMethod());
                            il.Emit(OpCodes.Nop);
                            il.MarkLabel(brtrue_label);
                        }
                    }

                    il.Emit(OpCodes.Ldloc, out_builder);
                    il.Emit(OpCodes.Ret);
                }
                _handler = (emit_load)method.CreateDelegate(typeof(emit_load));
            }

            public t_out map(object input)
            {
                return _handler(input);
            }

            public List<t_out> maps(IEnumerable inputs)
            {
                var list = new List<t_out>();
                foreach (var item in inputs)
                {
                    list.Add(_handler(item));
                }

                return list;
            }

            private static Type item_type(Type t)
            {
                if (t == null)
                {
                    return null;
                }

                if (t.IsGenericType)
                {
                    if (t.GenericTypeArguments != null && t.GenericTypeArguments.Length > 0)
                    {
                        return t.GenericTypeArguments[0];
                    }
                }

                if (t.IsArray)
                {
                    return t.Assembly.GetType(t.FullName.Trim('[', ']'));
                }

                return null;
            }

            private static bool is_value(Type t)
            {
                if (t == null)
                {
                    return false;
                }

                return t.IsValueType || t == typeof(string);
            }

            private static bool is_class(Type t)
            {
                if (t == null)
                {
                    return false;
                }

                return t.IsClass && !t.IsArray && !t.IsGenericType;
            }

            private static bool is_array(Type t)
            {
                if (t == null)
                {
                    return false;
                }

                return t.IsArray;
            }

            private static bool is_list(Type t)
            {
                if (t == null)
                {
                    return false;
                }

                return t.IsGenericType;
            }
        }
    }
}