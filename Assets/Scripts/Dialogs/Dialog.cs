using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Dialog{
    [SerializeField] List<string> lines;

    public bool isEmpty(){
        if (lines.Count == 0){
            return true;
        }
        return false;
    }

    public void initFirst(string line){
        lines = new List<string>();
        lines.Add(line);
    }

    public void append(string line){
        lines.Add(line);
    }

    public void prepend(string line){
        lines.Insert(0, line);
    }

    public bool replaceFirst(string line){
        if (line == null || line.Length == 0){
            return false;
        }
        lines[0] = line;
        return true;
    }

    public int count(){
        return lines.Count;
    }

    public string getFirst(){
        return lines[0];
    }

    public List<string> Lines{
        get {return lines;}
    }
}
