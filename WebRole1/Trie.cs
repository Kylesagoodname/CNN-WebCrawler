using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebRole1
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Web;

    /// <summary>
    /// Summary description for Class1
    /// </summary>
    public class Trie
    {
        private PerformanceCounter memProcess = new PerformanceCounter("Memory", "Available MBytes");
        public Trie()
        {

        }

        public Node build(String[] phrases)
        {
            Console.WriteLine("Beginning build process...");
            Node root = new Node("");
            //String phrase = "";

            //var filePath = System.IO.Path.GetTempPath() + "\\wiki.txt";
            //System.IO.StreamReader file = new System.IO.StreamReader(filePath);
            
            //while (getAvailableBytes() > 50)
            //{
            foreach(String phrase in phrases) {
                
            //    phrase = file.ReadLine();

                Node current = root;
                int count = 0;
                foreach (Char let in phrase)
                {
                    count++;
                    String letter = let.ToString().ToLower();
                    //If the current node's dictionary does not contain the letter
                    if (!current.getEdges().Keys.Contains(letter))
                    {
                        Node newEdge = new Node(letter);
                        current.addEdge(newEdge);

                        //Sets the newly added edge as the current node 
                        current = newEdge;

                    }
                    //If the current node's dictionary does contain the letter
                    else
                    {
                        Node prevNode = current;
                        current = prevNode.getEdge(letter);
                    }
                    //If we have reached the end of a phrase, add the full phrase to the node
                    if (count == phrase.Length)
                    {
                        current.addWord(phrase);
                    }
                }

            }
            return root;
        }

        public ArrayList traverse(Node root, String phrase, int search)
        {
            ArrayList empty = new ArrayList { "", "", "", "", "", "", "", "", "", "" };
            if (phrase == "")
            {
                return empty;
            }
            ArrayList output = new ArrayList();
            output.Clear();
            Node current = root;
            int count = 0;

            //Traverse to the end of the phrase and get the node of the last letter
            foreach (Char let in phrase)
            {
                count++;
                String letter = let.ToString();
                Node prevNode = current;

                //Handles unknown word case
                if (prevNode.getEdge(letter) == null)
                {
                    return empty;
                }
                current = prevNode.getEdge(letter);
            }

            int counter = 0;
            //Traverse down trie until we get 10 results
            while (counter < 10)
            {
                counter++;
                String result = helper(current, search);

                //add result to list
                output.Add(result.ToLower());
            }
            if (output[0] == "")
            {
                return empty;
            }

            return output;
        }


        public float getAvailableBytes()
        {
            float memUsage = memProcess.NextValue();
            return memUsage;
        }

        private static String helper(Node current, int search)
        {
            //If the current node is the end of a node and has not been visited
            if (current.isWord() && !current.seen() || current.isWord() && current.getNum() != search)
            {
                current.setNum(search);
                current.haveSeen();
                return current.getWord();
            }
            //If the node has children parse the dictionary
            else if (current.getEdges().Count > 0)
            {

                //Append the results of the different child elements
                String children = "";
                foreach (Node child in current.getEdges().Values)
                {
                    String edge = helper(child, search);
                    if (edge != "")
                    {
                        return edge;
                    }
                    children = children + edge;
                }
                return children;
            }
            //If the current node has no children 
            return "";
        }
    }
}