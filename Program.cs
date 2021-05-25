using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Security.Policy;
using System.CodeDom;
using System.Data;

namespace P1_WebCrawler
{
    class Program
    {
        static void Main(string[] args)
        {
            // check if the arguments were passed correctly
            if (args.Length == 0)
            {
                Console.WriteLine("No url passed in the application. One url followed by the number of hops are required as arguments.\n Application terminated.\n");
                System.Environment.Exit(1);
            }

            var startingUrl = args[0];
            int totalHops = Int32.Parse(args[1]);

            // create a list for storing all the visited urls - avoiding duplication
            List<string> visited = new List<string>();
            // create a list for storing the urls get from the current webpage
            List<string> urlList = new List<string>();
            // store the last HTML page content
            string resultPage = null;

            using (var client = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate }))
            {
                //client.Timeout = TimeSpan.FromMinutes(10);
                HttpResponseMessage response1 = client.GetAsync(startingUrl).Result;

                // check first url provided as an argument
                // 300 level error is automatically redirected, except for 300. 300 will be treated as 400 status code as Professor Dimpsey's suggestion
                if ((int)response1.StatusCode >= 400 && (int)response1.StatusCode < 500)
                {
                    Console.WriteLine("The url provided is not valid. Please use a valid url as argument. \nApplication terminated.");
                    System.Environment.Exit(1);
                }
                if ((int)response1.StatusCode == 300)
                {
                    Console.WriteLine("The webpage has multiple presentation. Since we can not figure out which one should be used as a console application and this is the very first url link, program is terminated.");
                    System.Environment.Exit(1);
                }
                else if ((int)response1.StatusCode >= 500)
                {
                    // retry 3 times to access, if not successful, terminate
                    int iteration = 0;
                    while ((int)response1.StatusCode >= 500 && iteration < 3)
                    {
                        response1 = client.GetAsync(startingUrl).Result;
                        ++iteration;
                    }
                    if (iteration > 2)
                    {
                        Console.WriteLine("There is some error occured on the server side. Please try again later. \nApplication terminated.");
                        System.Environment.Exit(1);
                    }
                }
                if ((int)response1.StatusCode >= 200 && (int)response1.StatusCode < 300)
                {
                    resultPage = response1.Content.ReadAsStringAsync().Result;
                    urlList = LinkParser(resultPage);
                    visited.Add(startingUrl);
                }

                // evaluate the rest of the urls and hops
                int hop = 0;
                int toCheck = 0;
                while (hop < totalHops && toCheck < urlList.Count)
                {
                    // if the url toCheck is already visited, then skip and evaluate the next
                    string urlToCheck = urlList[toCheck];
                    int len = urlToCheck.Length;
                    if (urlToCheck[len - 1] != '/')
                    {
                        if (visited.Contains(urlToCheck) || visited.Contains(urlToCheck + '/'))
                        {
                            ++toCheck;
                            continue;
                        }
                    }
                    if (urlToCheck[len - 1] == '/')
                    {
                        if (visited.Contains(urlToCheck) || visited.Contains(urlToCheck.Substring(0, len - 1)))
                        {
                            ++toCheck;
                            continue;
                        }
                    }

                    HttpResponseMessage response;
                    try
                    {
                        response = client.GetAsync(urlList[toCheck]).Result;
                        // 300 level error is automatically redirected, except for 300. 300 will be treated as 400 status code as Professor Dimpsey's suggestion
                        // 400 error code - increment to the next url on the urlList
                        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                            ++toCheck;
                        if ((int)response1.StatusCode == 300)
                        {
                            Console.WriteLine("The webpage has multiple presentation. Since we can not figure out which one should be used and this is not the staring url, we will skip this one and look for the next url.");
                            ++toCheck;
                        }
                        // 500 error code - retry 3 times, if still not success, then do nothing and increment to the next url on the urlList
                        else if ((int)response.StatusCode >= 500)
                        {
                            int iteration = 0;
                            while ((int)response.StatusCode >= 500 && iteration < 3)
                            {
                                response = client.GetAsync(startingUrl).Result;
                                ++iteration;
                            }
                            if (iteration >= 3)
                                ++toCheck;
                        }
                        // 200 success code - proceed
                        if ((int)response.StatusCode >= 200 && (int)response.StatusCode < 300)
                        {
                            Console.WriteLine($"url hopped to is: {urlList[toCheck]}");
                            resultPage = response.Content.ReadAsStringAsync().Result;
                            visited.Add(urlList[toCheck]);
                            // call LinkParser method to parse out all the URL in this page - return a string list of urls
                            urlList = LinkParser(resultPage);
                            // move on to the new list, need to reset toCheck
                            toCheck = 0;
                            ++hop;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                        ++toCheck;
                    }
                }
                // print out the last page
                if(urlList.Count == 0)
                    Console.WriteLine($"\n\nWe have reached to a webpage with no external links. Reached {hop} hops. The last HTML page is: \n{resultPage}\n");
                else if(toCheck >= urlList.Count && toCheck != 0 && toCheck != totalHops)
                    Console.WriteLine($"\n\nWe have ran out of the urls on the urlList we found on the current page. Reached {hop} hops. The last HTML page is: \n{resultPage}\n");
                else
                    Console.WriteLine($"\n\nReached {hop} hops. The last HTML page is: \n{resultPage}\n");
            }

        }


        static List<string> LinkParser(string Page)
        {
            List<string> temp = new List<string>();
            List<string> urlList = new List<string>();
            
            var linkParser = new Regex("<a.*?href=\"(https?.*?)\".*?");
            foreach (Match m1 in linkParser.Matches(Page))
                temp.Add(m1.Groups[1].ToString());

            // only add valid urls starting with http/https into the final urlList
            var validParser = new Regex(@"\b(?:https?://)\S+\b");
            for (int i = 0; i < temp.Count; ++i)
            {
                if (validParser.IsMatch(temp[i]))
                    urlList.Add(temp[i]);
            }

            //Console.WriteLine($"ulr List is here: \n");
            //urlList.ForEach(Console.WriteLine);
            return urlList;
        }
    }
}
