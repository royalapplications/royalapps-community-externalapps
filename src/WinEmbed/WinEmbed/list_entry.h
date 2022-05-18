#pragma once


//
//  Doubly-linked list manipulation routines.
//

//
//  VOID
//  InitializeListHead32(
//      PLIST_ENTRY32 ListHead
//      );
//

#define InitializeListHead32(ListHead) (\
	(ListHead)->Flink = (ListHead)->Blink = PtrToUlong((ListHead)))

#define RTL_STATIC_LIST_HEAD(x) LIST_ENTRY x = { &x, &x }

FORCEINLINE
VOID
InitializeListHead(
				   _Out_ PLIST_ENTRY ListHead
				   )

{

	ListHead->Flink = ListHead->Blink = ListHead;
	return;
}

_Must_inspect_result_
BOOLEAN
CFORCEINLINE
IsListEmpty(
			_In_ const LIST_ENTRY * ListHead
			)

{

	return (BOOLEAN)(ListHead->Flink == ListHead);
}

FORCEINLINE
BOOLEAN
RemoveEntryListUnsafe(
					  _In_ PLIST_ENTRY Entry
					  )

{

	PLIST_ENTRY Blink;
	PLIST_ENTRY Flink;

	Flink = Entry->Flink;
	Blink = Entry->Blink;
	Blink->Flink = Flink;
	Flink->Blink = Blink;
	return (BOOLEAN)(Flink == Blink);
}

FORCEINLINE
BOOLEAN
RemoveEntryList(
				_In_ PLIST_ENTRY Entry
				)

{

	PLIST_ENTRY Blink;
	PLIST_ENTRY Flink;

	Flink = Entry->Flink;
	Blink = Entry->Blink;
	Blink->Flink = Flink;
	Flink->Blink = Blink;
	return (BOOLEAN)(Flink == Blink);
}

FORCEINLINE
PLIST_ENTRY
RemoveHeadList(
			   _Inout_ PLIST_ENTRY ListHead
			   )

{

	PLIST_ENTRY Flink;
	PLIST_ENTRY Entry;

	Entry = ListHead->Flink;
	Flink = Entry->Flink;
	ListHead->Flink = Flink;
	Flink->Blink = ListHead;
	return Entry;
}



FORCEINLINE
PLIST_ENTRY
RemoveTailList(
			   _Inout_ PLIST_ENTRY ListHead
			   )

{

	PLIST_ENTRY Blink;
	PLIST_ENTRY Entry;

	Entry = ListHead->Blink;
	Blink = Entry->Blink;
	ListHead->Blink = Blink;
	Blink->Flink = ListHead;
	return Entry;
}


FORCEINLINE
VOID
InsertTailList(
			   _Inout_ PLIST_ENTRY ListHead,
			   _Inout_ __drv_aliasesMem PLIST_ENTRY Entry
			   )
{

	PLIST_ENTRY Blink;

	Blink = ListHead->Blink;
	Entry->Flink = ListHead;
	Entry->Blink = Blink;
	Blink->Flink = Entry;
	ListHead->Blink = Entry;
	return;
}


FORCEINLINE
VOID
InsertHeadList(
			   _Inout_ PLIST_ENTRY ListHead,
			   _Inout_ __drv_aliasesMem PLIST_ENTRY Entry
			   )
{

	PLIST_ENTRY Flink;

	Flink = ListHead->Flink;
	Entry->Flink = Flink;
	Entry->Blink = ListHead;
	Flink->Blink = Entry;
	ListHead->Flink = Entry;
	return;
}

FORCEINLINE
VOID
AppendTailList(
			   _Inout_ PLIST_ENTRY ListHead,
			   _Inout_ PLIST_ENTRY ListToAppend
			   )
{

	PLIST_ENTRY ListEnd = ListHead->Blink;

	ListHead->Blink->Flink = ListToAppend;
	ListHead->Blink = ListToAppend->Blink;
	ListToAppend->Blink->Flink = ListHead;
	ListToAppend->Blink = ListEnd;
	return;
}
