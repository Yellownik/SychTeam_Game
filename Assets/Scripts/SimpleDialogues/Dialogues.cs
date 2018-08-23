﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue", menuName = "Dialogue")]
public class Dialogues : ScriptableObject
{
	public enum WindowTypes { Text, Choice, ChoiceAnswer }
	public enum NodeType { Start, Default, End }

	private Window currentWindow;

	public List<WindowSet> Set = new List<WindowSet>();
	public int CurrentSet = 0;

	[System.Serializable]
	public class WindowSet
	{
        public string Name;
        public int Importance = 0;

		public int CurrentId;
		public int FirstWindow = -562;
		public List<Window> Windows = new List<Window>();
        public QuestCondition showCondition = new QuestCondition();

        public WindowSet(string name)
		{
            Name = name;
			CurrentId = 0;
		}

		public Window GetWindow(int ID)
		{
			return Windows.FirstOrDefault(w => w.ID == ID);
		}

		public int GetWindowIndex(int ID)
        { 
			return Windows.FindIndex(w => w.ID == ID);
        }
	}

	[System.Serializable]
	public class Window
	{
		public int ID;

		public Rect Size;
		public string Text;
		public WindowTypes Type;
		public NodeType NodeType;
		public int Parent;
		public Speaker speaker;

		public List<QuestState> activateQuests = new List<QuestState>();
		public QuestCondition showCondition = new QuestCondition();

		public List<int> Connections = new List<int>();

		public Window(int id, int parent, Rect newSize, 
			WindowTypes type = WindowTypes.Text, NodeType nodeType = NodeType.Default)
		{
			ID = id;
			Parent = parent;
			Size = newSize;
			Text = "";
			Type = type;
			NodeType = nodeType;
		}

		public bool IsChoice()
		{
			return Type == WindowTypes.Choice;
		}
	}

    /// <summary>
    /// Set the current node back to the beginning
    /// </summary>
    /// <returns></returns>
    private void Reset()
    {
        currentWindow = Set[CurrentSet].Windows[Set[CurrentSet].FirstWindow];
    }

    public string[] TabsNames
    {
        get
        {
            return Set.Select(set => set.Name)
                .ToArray();
        }
    }

    public void SetFirstTree(int treeIndex)
    {
        CurrentSet = treeIndex;
        Reset();
    }

    public int GetNextTreeIndex(int treeIndex)
    {
        return (treeIndex + 1) % Set.Count;
    }

	public bool End()
	{
		return currentWindow.Connections.Count == 0;
	}

	public Speaker CurrentSpeaker()
	{
		return currentWindow.speaker;
	}

	/// <summary>
	/// Moves to the next item in the list.
	/// </summary>
	/// <returns># = Amount of choices it has | 0 = success | -1 = end</returns>
	public int Next()
	{
		ServiceLocator.QuestSystem.SetQuestProgress(currentWindow.activateQuests);

		if (currentWindow.Type == WindowTypes.Choice)
			return currentWindow.Connections.Count;
		else 
			if (currentWindow.Connections.Count == 0)
				return -1;
			else
			{
				currentWindow = Set[CurrentSet].GetWindow(currentWindow.Connections[0]);
				return 0;
			}
	}

	/// <summary>
	/// Returns the choices the current node has. 
	/// </summary>
	/// <returns>null if the node isn't a decision node. An array of strings otherwise</returns>
	public string[] GetChoices()
	{
		if (currentWindow.Type != WindowTypes.Choice)
			return new string[] { };
		else
		{
			List<Window> options = new List<Window>();
			for (int i = 0; i < currentWindow.Connections.Count; i++)
			{
				options.Add(Set[CurrentSet].GetWindow(currentWindow.Connections[i]));
			}

			var optionsForShow = options.Where(w => w.showCondition.GetConditionValue());
			var choises = optionsForShow.OrderBy(w => w.Size.y)
				.Select(w => w.Text)
				.ToArray();

			return choises;
		}
	}

	/// <summary>
	/// Moves to the next selected choice
	/// </summary>
	/// <param name="choice"></param>
	/// <returns></returns>
	public bool NextChoice(string choice)
	{
		if (currentWindow.Type != WindowTypes.Choice)
			return false;
		else
		{
			for (int i = 0; i < currentWindow.Connections.Count; i++)
			{
				if (Set[CurrentSet].GetWindow(currentWindow.Connections[i]).Text == choice)
				{
					ServiceLocator.QuestSystem.SetQuestProgress(currentWindow.activateQuests);

					currentWindow = Set[CurrentSet].GetWindow(Set[CurrentSet].GetWindow(currentWindow.Connections[i]).Connections[0]);
					return true;
				}
			}
			return false;
		}
	}

	public string GetCurrentDialogue()
	{
		if (currentWindow == null)
			Reset();
		return currentWindow.Text;
	}
}
