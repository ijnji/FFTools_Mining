using System;
using System.Collections.Generic;

namespace FFTools {
    public class PriorityQueue <T> where T : IComparable {
        //priority queue implemented with list starting at index 1 for easier maths
        //minimum value has highest priority
        //parent at i
        //left child at 2i
        //right child at 2i+1
        protected List <T> list;
        
        public PriorityQueue() {
            list = new List <T> ();
            list.Add(default (T)); //dummy at index 0
        }

        public int Count {
            get {
                return list.Count - 1;
            }
        }

        public void addNew(T newEntry) {
            list.Add(newEntry);
            bubbleUp();
        }

        public T removeTop() {
            if(this.Count == 0)
                return default(T);

            T returnT = list[1];
            list[1] = list[list.Count-1];
            list.RemoveAt(list.Count-1);

            percolateDown();

            return returnT;
        }

        private void bubbleUp() {
            //start at bottom
            int index = this.Count;
            if (index <= 1) return;

            bool swap = list[index/2].CompareTo(list[index]) >= 0;       //negative: instance precedes obj; zero: same; positive: instance follows obj
                                                                         //using >= so that latest addition takes priority (greedy)
            while(swap && !(index <= 1)) {
                T temp = list[index];
                list[index] = list[index/2];
                list[index/2] = temp;
                index = index / 2;
                swap = list[index/2].CompareTo(list[index]) >= 0;       //negative: instance precedes obj; zero: same; positive: instance follows obj
                                                                        //using >= so that latest addition takes priority (greedy)
            }
        }

        private void percolateDown() {
            int index = 1; //start at top
            if (this.Count <= 1) return;

            bool leftExists, rightExists;
            int checkLeft, checkRight, checkLvsR;
            bool swapLeft, swapRight;

            leftExists = (2*index) <= this.Count;
            rightExists = (2*index + 1) <= this.Count;

            swapLeft = false; swapRight = false;
            if (leftExists) {
                checkLeft = list[index].CompareTo(list[2*index]);
                swapLeft = checkLeft > 0;
            }
            if (rightExists) {
                checkRight = list[index].CompareTo(list[2*index + 1]);
                swapRight = checkRight > 0;
            }
            if(leftExists && rightExists) {
                checkLvsR = list[2*index].CompareTo(list[2*index + 1]);
                if (checkLvsR < 0) {
                    //left has higher priority
                    swapRight = false;
                } else {
                    //right has higher priority
                    swapLeft = false;
                }
            }

            while(swapLeft || swapRight) {
                if (swapRight) {
                    T temp = list[index];
                    list[index] = list[2*index + 1];
                    list[2*index + 1] = temp;
                    index = 2*index + 1;
                }
                else if (swapLeft) {
                    T temp = list[index];
                    list[index] = list[2*index];
                    list[2*index] = temp;
                    index = 2*index;
                }
       
                leftExists = (2*index) <= this.Count;
                rightExists = (2*index + 1) <= this.Count;

                swapLeft = false; swapRight = false;
                if (leftExists) {
                    checkLeft = list[index].CompareTo(list[2*index]);
                    swapLeft = checkLeft > 0;
                }
                if (rightExists) {
                    checkRight = list[index].CompareTo(list[2*index + 1]);
                    swapRight = checkRight > 0;
                }
                if(leftExists && rightExists) {
                    checkLvsR = list[2*index].CompareTo(list[2*index + 1]);
                    if (checkLvsR < 0) {
                        //left has higher priority
                        swapRight = false;
                    } else {
                        //right has higher priority
                        swapLeft = false;
                    }
                }
            }
        }
    }
}