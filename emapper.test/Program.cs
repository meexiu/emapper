using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace emapper.test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("...");

            var a = new aclass
            {
                name = "yj",
                age = 18,
                add_time = new DateTime(1990, 2, 5),
                status = false,
                scores = new int[] { 1, 2, 3, 4, 5, 6, 7, 8 },
                address = new string[] { "浙江省", "杭州市", "西湖区", "西湖国际", null },
                score_list = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 },
                child = new a1class
                {
                    name = "child-mixiu",
                    age = 2,
                    add_time = new DateTime(2018, 12, 25),
                    status = true
                },
                items = new List<a1class>
                {
                    new a1class
                    {
                        name = "list-mixiu1",
                        age = 1,
                        add_time = new DateTime(2020, 1, 7),
                        status = true
                    },
                    new a1class
                    {
                        name = "list-mixiu2",
                        age = 2,
                        add_time = new DateTime(2020, 1, 8),
                        status = false,
                        next = new a1class
                        {
                            name = "list-mixiu2-next1",
                            age = 1,
                            add_time = new DateTime(2022, 1, 8),
                            status = true
                        }
                    }
                },
                item_array = new a1class[]
                {
                    new a1class
                    {
                        name = "array-mixiu1",
                        age = 11,
                        add_time = new DateTime(2020, 1, 7),
                        status = true
                    },
                    new a1class
                    {
                        name = "array-mixiu2",
                        age = 12,
                        add_time = new DateTime(2020, 1, 8),
                        status = false
                    },
                    new a1class
                    {
                        name = "array-mixiu3",
                        age = 13,
                        add_time = new DateTime(2020, 1, 8),
                        status = false,
                        next = new a1class
                        {
                            name = "array-mixiu3-next1",
                            age = 11,
                            add_time = new DateTime(2023, 1, 8),
                            status = true,
                            next = new a1class
                            {
                                name = "array-mixiu3-next1-next1",
                                age = 11111,
                                add_time = new DateTime(2028, 1, 8),
                                status = true
                            }
                        }
                    }
                }
            };

            for (int i = 0; i < 10000; i++)
            {
                a.items.Add(new a1class
                {
                    name = "list-mixiu1" + "-" + i,
                    age = 1,
                    add_time = new DateTime(2020, 1, 7),
                    status = true
                });
            }

            var b = emapper.map<bclass>(a);

            Stopwatch sw = new Stopwatch();
            sw.Start();

            for (int i = 0; i < 1000; i++)
            {
                emapper.map<bclass>(a);
            }

            sw.Stop();
            Console.WriteLine(sw.ElapsedMilliseconds + "ms");

            Console.ReadLine();
        }
    }

    public class aclass
    {
        public string name { get; set; }

        public int age { get; set; }

        public DateTime add_time { get; set; }

        public bool status { get; set; }

        public string[] address { get; set; }

        public int[] scores { get; set; }

        public List<int> score_list { get; set; }

        public a1class child { get; set; }

        public List<a1class> items { get; set; }

        public a1class[] item_array { get; set; }
    }

    public class bclass
    {
        public string name { get; set; }

        public int age { get; set; }

        public DateTime add_time { get; set; }

        public bool status { get; set; }

        public string[] address { get; set; }

        public int [] scores { get; set; }

        public List<int> score_list { get; set; }

        public b1class child { get; set; }

        public List<b1class> items { get; set; }

        public b1class[] item_array { get; set; }
    }

    public class a1class
    {
        public string name { get; set; }

        public int age { get; set; }

        public DateTime add_time { get; set; }

        public bool status { get; set; }

        public a1class next { get; set; }
    }

    public class b1class
    {
        public string name { get; set; }

        public int age { get; set; }

        public DateTime add_time { get; set; }

        public bool status { get; set; }

        public b1class next { get; set; }
    }
}
