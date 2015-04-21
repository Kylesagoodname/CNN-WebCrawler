using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

/// <summary>
/// Summary description for Class1
/// </summary>
/// 
namespace WebRole1
{
    public class Node
    {
        private string letter;
        private string word;
        private Dictionary<string, Node> edges;
        private Boolean visited;
        private int num;
        public Node(String letter)
        {
            this.letter = letter;
            word = "";
            edges = new Dictionary<string, Node>();
            visited = false;
            num = 0;

        }

        public string getLetter()
        {
            return this.letter;
        }

        public Boolean isWord()
        {
            return word != "";
        }

        public void addEdge(Node input)
        {
            edges.Add(input.getLetter(), input);
        }

        public Dictionary<String, Node> getEdges()
        {
            return edges;
        }

        public Node getEdge(String input)
        {
            if (edges.ContainsKey(input))
            {
                return edges[input];
            }
            return null;
        }
        public void addWord(String input)
        {
            word = input;
        }

        public String getWord()
        {
            return word;
        }

        public Boolean seen()
        {
            return visited;
        }

        public void haveSeen()
        {
            visited = true;
        }

        public int getNum()
        {
            return num;
        }

        public void setNum(int input)
        {
            num = input;
        }

    }
}