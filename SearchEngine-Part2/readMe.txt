readMe file:
The assignment is written in C#.

Inside Ravid_Dor Folder you will find all our AfterPosting, Cache and Dictionary Files and our offline
files, please put the corpus and stop_words in this folder and LOAD it with PART 2 LOAD button.

As for the GUI:

***********Beside Stemming - PART A- Irrelevant***********
- "Corpus Folder Browser" - The path to the folder that holds the corpus.
- "Posting Saving Location" - The path to where the posting-files will be saved.
- "Stemming" check-box - For your decision, stems using Porter's algorithm.
- "Start Working" - Starts parsing, indexing and making posting files.
- "Reset" - Frees all memory allocated, deletes dictionary all posting file
- "Save Cache" - Saves the cache file with .chex suffix to a choosed folder
- "Save Dictionary" - Saves the dictionary with .dicx suffix to a choosed folder
- "Show Cache"/"Show Dictionary" - Opens the cache/dictionary with notepad++
- "Load Cache"/"Load Dictionary" - Loads the cache/dictionary file ONLY with the right suffix


***********PART 2***********
- "Stemming" check-box - For your decision, stemming using Porter's algorithm.
- "Wikipedia Expand" check-box- For your decision, using Wikipedia's API to add terms to expand the query. Working only on one-word query.
- "Document Summarize" check-box- For your decision, if the query is a document name, take the most significant 5 senescence print and score them. 
- "Browser for query file.." - choose a query file format and search the queries in it, if there is a description, it will add up to the query. 
	once you choose file, YOU MUST RUN OR Reset IN ORDER TO SEARCH AGAIN.
- "Run" - search a query, available after PART 2 LOAD.
- "Reset part 2" - clear all the GUI as requested. Delets the last saved result file.
- "Load Part 2"- Fast loading button in order to run queries immediately. As listed before you should have all the AFTER-POSTING files, and our files in order to run properly. 
	loads the files accordingly to the Stemming check-box, if you would like to change the Stemming you should LOAD AGAIN.
 
**Result Window**
-"Save"- If it's NOT a Summary query, you can save your results in a TrecEval format using this button. 

- Every working action (Save/Load/Start/RUN) will only be complete with a pop-up windows, please wait patiently for the pop-up.
Meanwhile - the GUI will not be available.

- For the Show Dictionary/Show Cache - NOTEPAD++ IS NEEDED!

- You can run through the visual project or through the .exe, we prefer the visual because of the Debug Console (we had some prints to see what the state of the program) 

The assignment ran smoothly on computer 14 at lab -101, bulding 96.

ENJOY!
Ravid_Dor
