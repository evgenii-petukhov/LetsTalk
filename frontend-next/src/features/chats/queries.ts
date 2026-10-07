import { useInfiniteQuery, useQuery } from "@tanstack/react-query";
import { api } from "@/lib/api/client";

export function useChats() {
    return useQuery({
        queryKey: ["chats"],
        queryFn: () => api.chatAll(),
    });
}

// Page 0 holds the latest messages; each following page is the next older block.
// An empty page means the beginning of the conversation has been reached.
export function useMessages(chatId: string | null) {
    return useInfiniteQuery({
        queryKey: ["messages", chatId],
        queryFn: ({ pageParam }) => api.messageAll(chatId!, pageParam),
        initialPageParam: 0,
        getNextPageParam: (lastPage, allPages) =>
            lastPage.length === 0 ? undefined : allPages.length,
        select: (data) => ({
            pageParams: data.pageParams,
            // Oldest block first, each block already oldest -> newest; sort defensively.
            messages: [...data.pages]
                .reverse()
                .flat()
                .sort((a, b) => (a.created ?? 0) - (b.created ?? 0)),
        }),
        enabled: chatId !== null,
    });
}
