// Copyright (c) Microsoft Corporation.  All rights reserved.
using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using PipelineBuilder;


namespace VSPipelineBuilder
{
    [Serializable]
    internal class CheckSumValidator
    {
        Dictionary<String, byte[]> _files;
        string _path;
        private CheckSumValidator(String path)
        {
            _path = path;
            _files = new Dictionary<string, byte[]>();
            string[] fileList = Directory.GetFiles(_path, "*.cs");
            foreach (String file in fileList)
            {
                byte[] hash = System.Security.Cryptography.SHA256.Create().ComputeHash(File.ReadAllBytes(file));
                 _files.Add(file, hash);
            }
        }

        private void WriteResults()
        {
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(_path+"\\checksum",FileMode.Create);
            formatter.Serialize(stream,this);
            stream.Close();
        }

        public static bool ValidateCheckSum(string path)
        {
            CheckSumValidator current = new CheckSumValidator(path);
            CheckSumValidator original = GetInfo(path);
            if (original == null)
            {
                //There is no pre-computed check-sum. 
                return true;
            }
            return !current.HasEdits(original);
        }

        public static void StoreCheckSum(string path)
        {
            new CheckSumValidator(path).WriteResults();
        }

		/// <summary>
		/// Checks for edit and return true if there were edits.
		/// </summary>
		/// <param name="original"></param>
		/// <returns></returns>
        private bool HasEdits(CheckSumValidator original)
        {
            foreach (string s in original._files.Keys)
            {
            	byte[] currentHash;
            	if (!_files.TryGetValue(s, out currentHash)) continue;
            	
				byte[] originalHash = original._files[s];
				
            	if (currentHash.Length == originalHash.Length &&
            	    originalHash.AllTrue((index, currByte) => currentHash[index].Equals(currByte))) 
					continue;

            	if (MessageBox.Show(string.Format(
            	                	"File has been edited and left in the ''Generated Files'' folder. Would you like " 
            	                	+ "to overwrite it? File: {0}.", s),
            	                "Edited file in generated folder",
            	                MessageBoxButtons.YesNo) == DialogResult.Yes)
            	{
            		continue; // continue checking, ignore this file.
            	}
            	
				return true;
            }

        	foreach (var s in _files.Keys)
            {
            	if (original._files.ContainsKey(s)) continue;

            	if(MessageBox.Show(string.Format(
            	                   	"File has been added to the \"Generated Files\" folder manually. "
									+ "Would you like to overwrite it? File: {0}.", s),
            	                   "Added file in generated folder",
            	                   MessageBoxButtons.YesNo) == DialogResult.Yes)
            	{
            		continue; // continue checking, ignore this file.
            	}

            	return true;
            }

			return false;
        }

        private static CheckSumValidator GetInfo(string path)
        {
            if (!File.Exists(path+"\\checksum"))
            {
                return null;
            }
            BinaryFormatter formatter = new BinaryFormatter();
            FileStream stream = new FileStream(path + "\\checksum", FileMode.Open);
            CheckSumValidator result = (CheckSumValidator)formatter.Deserialize(stream);
            stream.Close();
            return result;

        }
    }
}