using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using CoreTweet;
using $safeprojectname$.Properties;

namespace $safeprojectname$
{
	public class Program
    {
        static void Main(string[] args)
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;


            //uncomment this to clear user settings and reauthorize
            //Properties.Settings.Default.Reset();

            //load a the twitter application settings to be used later, these come from a user settings file, and will be blank by default
            var consumerKey = Settings.Default["consumerKey"].ToString();
            var consumerSecret = Settings.Default["consumerSecret"].ToString();


            //if we don't have these stores yet, get them from the console, these are stored in a user settings file for subsequent calls
            if (consumerKey == string.Empty || consumerSecret == string.Empty)
            {
                //we need to get these from our application in twitter
                Console.WriteLine("Enter your consumerKey:");
                consumerKey = Console.ReadLine();

                Console.WriteLine("Enter your consumerSecret:");
                consumerSecret = Console.ReadLine();
            }

            //load the rest of the settings
            var session = OAuth.Authorize(consumerKey, consumerSecret);
            var accessToken = Settings.Default["accessToken"].ToString();
            var accessSecret = Settings.Default["accessSecret"].ToString();
            long userId = (long)Settings.Default["userID"];
            var screenName = Settings.Default["screenName"].ToString();

            Tokens tokens;

            if (consumerKey != string.Empty && consumerSecret != string.Empty && accessToken != string.Empty &&
                accessSecret != string.Empty)
            {
                //if we already have the settings, let's create the auth token
                //Create(string consumerKey, string consumerSecret, string accessToken, string accessSecret, long userID = 0, string screenName = null);
                tokens = Tokens.Create(consumerKey, consumerSecret, accessToken, accessSecret, userId, screenName);
            }
            else
            {
                //If we don't have these settings populated already we need to go authenticate the user with twitter and get a pin code
                //need to authenticate, and then get a pin code from Twitter to store/use
                System.Diagnostics.Process.Start(session.AuthorizeUri
                    .AbsoluteUri); //this will open up a URL for the user to login/authorize the app
                Console.WriteLine("To Activate @DevUpBot Enter the Pin code from Twitter");
                string pin = Console.ReadLine();

                tokens = session.GetTokens(pin);
                //save the token values to our settings, when saved properly we don't have to reauthorize the app when starting it up again.
                Settings.Default["consumerKey"] = tokens.ConsumerKey;
                Settings.Default["consumerSecret"] = tokens.ConsumerSecret;
                Settings.Default["accessToken"] = tokens.AccessToken;
                Settings.Default["accessSecret"] = tokens.AccessTokenSecret;
                Settings.Default["userID"] = tokens.UserId;
                Settings.Default["screenName"] = tokens.ScreenName;
                Settings.Default.Save();
            }

            //now let's look at getting the bot doing something

            //Write out a header to the console before we do anything real.
            Console.WriteLine("Tweet History:");

            //check if we have a lastTweetTime in the settings, this will be used to make sure we don't abuse the twitter API
            if (Settings.Default["lastTweetTime"].ToString() != string.Empty)
            {
                lastTweetTime = Convert.ToDateTime(Settings.Default["lastTweetTime"]);
            }

            //last tweet id will be passed into the Twitter Search API call to limit the results. We don't necessarily want to go back and reply to old tweets
            long lastTweetId = (long)Settings.Default["lastTweetId"];

            //This is where you need to start being careful of how often you do things, don't abuse!
            //This thing will run until you kill the console app with Control-C
            do
            {
                try
                {

                    //list of strings to use in replies, you can see more here, or change these
                    var listResponses = new List<string>
                        {

                            "@{0} this is my response to your tweet", "@{0} I'm a bot and think you're great!"
                        };

                    //check every 5 minutes to see if there is something new
                    if (DateTime.Now >= lastTweetTime.AddMinutes(5))
                    {

                        //randomize what text to use as a reply
                        int index = new Random().Next(listResponses.Count);
                        var status = listResponses[index];


                        // create a list of words to filter out of our search results. This came from @TheAnswerYes, we didn't want to actually reply YES if someone was thinking bad thoughts
                        var ignoreWords = "-kill -death -suicide -shoot -stab -kms -die -jump";

                        //look for tweets with CHRISTOC in the body, but ignore any that have some IGNORE keywords in them (replace the string with whatever you want to search for)
                        var res = tokens.Search.Tweets("\"christoc\""
                            , null, null, null, null
                            , null, null, lastTweetId
                            , null, null
                            , null
                            , null);

                        foreach (Status r in res.OrderBy(x => x.Id))
                        {
                            //Check to make sure we don't reply to a previously replied tweet, or to a specific account. 
                            if (r.Id != lastTweetId && r.User.ScreenName.ToLower() != "YOURBOTNAMEHERE")
                            {
                                lastTweetId = r.Id;
                                Settings.Default["lastTweetId"] = lastTweetId;
                                Settings.Default.Save();
                                status = string.Format(status, r.User.ScreenName);
                                tokens.Statuses.Update(
                                    status: status
                                    , in_reply_to_status_id: lastTweetId
                                );

                                Console.WriteLine("Reply to tweet from:" + r.User.ScreenName);

                                //if you want to reply to all tweets found, uncomment the following 3 lines
                                //index = new Random().Next(listResponses.Count);
                                //status = listResponses[index]; 
                                //break;
                            }
                        }

                        //store the last tweet time so we don't reply to that again in the future
                        lastTweetTime = DateTime.Now;
                        Settings.Default["lastTweetTime"] = lastTweetTime.ToString();
                        Settings.Default.Save();
                        Console.WriteLine("Last reply time: " + lastTweetTime);

                    }
                }
                catch (Exception ex)
                {
                    //if there was an error, write it to the screen, save the last tweet time and continue the do/while so that app doesn't completely crash.
                    Console.WriteLine(ex.InnerException);
                    Console.WriteLine(ex.Message);
                    lastTweetTime = DateTime.Now;
                    Settings.Default["lastTweetTime"] = lastTweetTime.ToString();
                    Settings.Default.Save();

                    Console.WriteLine("Error at:" + lastTweetTime);
                }
            } while (true);

        }
    }
}
