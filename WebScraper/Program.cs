using Docker.DotNet.Models;

using Humanizer.Localisation;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WebScraper
{
    public class Program
    {
        static void Main(string[] args)
        {
            IWebDriver driver = new ChromeDriver();

            driver.Navigate().GoToUrl("http://www.lawyerinfo.co.il/browse/locality");
            //Get town container
            var local = driver.FindElement(By.Id("browse-by-locality"));
            //Get all town link from town container
            var children = local.FindElements(By.TagName("a"));

            //This is so I can view hebrew text in console instead of seeing ???
            Console.OutputEncoding = Encoding.GetEncoding("Windows-1255");

            //list of all lawyers
            List<Lawyer> lawyersLi = new List<Lawyer>();


            //loop to enter each town, starts from 3 because of empty first 3 listings,
            //can be adjusted for the range of the ecxact city we need lawyers from

            //towncount is 953 , represents all the towns on the page
            var townCount = children.Count();
            //Manually adjusted range
            //townCount = 8;

            try
            {
                for (var i = 1; i < townCount; i++)
                {

                    children[i].Click();
                    //Thread.Sleep(1000);

                    //silly code that checks that element exists
                    List<IWebElement> isResults = new List<IWebElement>();
                    isResults.AddRange(driver.FindElements(By.Id("results")));

                    //check if there is next page
                    List<IWebElement> isNext = new List<IWebElement>();

                    //if the page contains at least one lawyer
                    if (isResults.Count != 0)
                    {
                        //get a list of all lawyers links
                        var results = driver.FindElement(By.Id("results"));
                        var law = results.FindElements(By.TagName("a"));

                        //for each lawyer in page
                        for (var k = 0; k < law.Count(); k++)
                        {
                            law[k].Click();
                            //locate the name and the address for each lawyer - they are pretty easy to reach
                            //only 1 h1 element on page and address has a class of its own
                            var name = driver.FindElement(By.TagName("h1")).Text;
                            var address = driver.FindElement(By.ClassName("address")).Text;
                            string cleaned = address.Replace("\n", "-").Replace("\r", "-");//prevent new line appearance in Excel
                            address = cleaned;

                            //initialize all lawyer personal information with empty strings
                            string tmpPhone = " -- ";
                            string tmpFax = " -- ";
                            string tmpMobile = " -- ";
                            string tmpMail = " -- ";
                            string tmpSpecialize = " - - ";

                            var fields = driver.FindElements(By.ClassName("field"));

                            //This takes care of personal info by getting all field objects(That used for personal info only)
                            foreach (var f in fields)
                            {
                                //each field contains one label that has the title of the info att.
                                var label = f.FindElement(By.ClassName("label"));

                                var tmp = Reverse(label.Text);

                                //Later on the data can be reached by collecting it from the data class from each field object
                                if (tmp == ":ןופלט")
                                {
                                    tmpPhone = f.FindElement(By.ClassName("data")).Text;
                                }
                                if (tmp == ":סקפ")
                                {

                                    tmpFax = f.FindElement(By.ClassName("data")).Text;
                                }
                                if (tmp == ":דיינ")
                                {

                                    tmpMobile = f.FindElement(By.ClassName("data")).Text;
                                }
                                if (tmp == ":ל\"דוא") ;
                                {

                                    tmpMail = f.FindElement(By.ClassName("data")).Text;
                                }

                            }


                            //handle specialities by finding all p TagNames- only the specialties use 'p' as a tag name.
                            List<IWebElement> isSpecialize = new List<IWebElement>();
                            isSpecialize.AddRange(driver.FindElements(By.TagName("p")));
                            //Check whether the lawyer has any, if he does just add it to the string
                            if (isSpecialize.Count > 0)
                            {
                                var p = driver.FindElements(By.TagName("p"));
                                if (isSpecialize.Count == 1)
                                {
                                    tmpSpecialize = p[0].Text;
                                }
                                else
                                {
                                    foreach (var s in p)
                                    {
                                        tmpSpecialize = tmpSpecialize + " and" + s.Text;
                                    }
                                }

                            }

                            //Create new Layer object and add to the list
                            Lawyer lawyer = new Lawyer(name, address, tmpPhone, tmpFax, tmpMobile, tmpMail, tmpSpecialize);
                            lawyersLi.Add(lawyer);

                            //Head back to city page
                            driver.Navigate().Back();


                            //target frame detuched annoying bug fix with try and catch maybe line 152
                            try
                            {
                                Thread.Sleep(2000);
                                results = driver.FindElement(By.Id("results"));
                            }
                            catch (WebDriverException e)
                            {
                                Console.WriteLine("ERORR CAUGHT" + e);
                                Thread.Sleep(1000);
                                driver.Navigate().Refresh();
                                try
                                {
                                    Console.WriteLine("Try again after refresh fails");
                                    results = driver.FindElement(By.Id("results"));
                                }
                                catch (WebDriverException)
                                {
                                    Console.WriteLine("I failed twice - Doing continie !!");
                                    continue;
                                }

                            }
                            law = results.FindElements(By.TagName("a"));

                            //when the last lawyer on current page was saved 
                            if (k == 19)
                            {
                                isNext.AddRange(driver.FindElements(By.LinkText("הבא")));
                                //Make sure there is a next page to go to
                                if (isNext.Count != 0)
                                {
                                    driver.FindElement(By.LinkText("הבא")).Click();
                                    Thread.Sleep(2000);

                                    //start from zero on lawyers count because at the end k++ happens
                                    k = -1;
                                    //Make range 0 again
                                    isNext.Clear();

                                    //get all lawyers again
                                    try
                                    {
                                        results = driver.FindElement(By.Id("results"));
                                    }
                                    catch (WebDriverException e)
                                    {
                                        Console.WriteLine("ERORR CAUGHT" + e);
                                        Thread.Sleep(1000);
                                        driver.Navigate().Refresh();
                                        try
                                        {
                                            Console.WriteLine("Try again after refresh fails");
                                            results = driver.FindElement(By.Id("results"));
                                        }
                                        catch (WebDriverException)
                                        {
                                            Console.WriteLine("I failed twice - Doing continie !!");
                                            continue;
                                        }

                                    }
                                    law = results.FindElements(By.TagName("a"));
                                }
                            }
                        }
                        isResults.Clear();
                    }



                    //Connect all new loaded elements to DOM
                    driver.Navigate().GoToUrl("http://www.lawyerinfo.co.il/browse/locality");
                    Thread.Sleep(1000);
                    local = driver.FindElement(By.Id("browse-by-locality"));
                    children = local.FindElements(By.TagName("a"));




                }
            }
            catch(WebDriverException e)
            {
                Console.WriteLine("I ran into fatal exception" + e);
                SaveObjectAsCSV(lawyersLi, @"C:\Users\Public\Documents\LawyerInfo.csv");
                return;
            }



            foreach(var l in lawyersLi)
            {
                Console.WriteLine("name : " + l.name + "\n address: " + l.address + "\n phone :" +
                    l.phone + "\n fax : " + l.fax + "\n mobile : " + l.mobile + "\n mail :" +
                    l.mail + "\n specialize : " + l.specialize+"\n ---------------------");
            }
            
            //Writing list to CSV - Here the path can be configured
            SaveObjectAsCSV(lawyersLi, @"C:\Users\Public\Documents\LawyerInfo.csv");
            
            //Exiting Chrome
            driver.Close();



        }

    

     
        //function to export list of lawyer objects to CSV format
        public static void SaveObjectAsCSV(List<Lawyer> arrayToSave, string fileName)
        {
            //problems of viweing file in hebrew solved : https://www.youtube.com/watch?v=vR2GWdmdl18

            using (StreamWriter file = new StreamWriter(fileName))
            {
                file.WriteLine("שם;כתובת;טלפון;פקס;נייד;אימייל;התמחות;הערות");
                
                foreach (var item in arrayToSave)
                {
                    var line = String.Format("{0};{1};{2};{3};{4};{5};{6}", item.name, item.address,
                        item.phone,item.fax,item.mobile,item.mail,item.specialize);
                    file.WriteLine(line + ";");
                    
                }
            }
        }


        //Lawyer object class with all fields needed to be exported to CSV
        public class Lawyer
        {

            public string name;
            public string address;
            public string phone;
            public string fax;
            public string mobile;
            public string mail;
            public string specialize;
            
            public Lawyer(string name,string address,string phone, string fax , string mobile ,string mail,string specialize)
            {
                this.name = name;
                this.address = address;
                this.phone = phone;
                this.fax = fax;
                this.mobile = mobile;
                this.mail = mail;
                this.specialize = specialize;
            }

        }

        //This function helps with printing to console in the right order (RTL)
        public static string Reverse(string s)
        {
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }




    }
}
