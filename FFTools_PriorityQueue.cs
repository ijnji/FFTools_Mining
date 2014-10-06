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
		
		public PriorityQueue()
		{
			list = new List <T> ();
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
            int index = list.Count - 1; 
            if (index == 0) return;

            bool swap = list[index/2].CompareTo(list[index]) > 0;       //negative: instance precedes obj; zero: same; positive: instance follows obj
            while(swap) {
                T temp = list[index];
                list[index] = list[index/2];
                list[index/2] = temp;
                index = index / 2;
            }
        }

        private void percolateDown() {
            int index = 1; //start at top
            if (list.Count == 1) return;

            int checkLeft, checkRight;
            bool swapLeft, swapRight;
            checkLeft = list[index].CompareTo(list[2*index]);           //negative: instance precedes obj; zero: same; positive: instance follows obj
            checkRight = ((2*index + 1) >= list.Count) ? -1 : index.CompareTo(list[2*index + 1]);
            swapLeft = checkLeft > 0;
            swapRight = checkRight > 0;

            while(swapLeft || swapRight) {
                if (swapRight) {
                    T temp = list[index];
                    list[index] = list[2*index + 1];
                    list[2*index + 1] = temp;
                    index = 2*index + 1;
                }
                if (swapLeft) {
                    T temp = list[index];
                    list[index] = list[2*index];
                    list[2*index] = temp;
                    index = 2*index;
                }
            }
        }
	}
}