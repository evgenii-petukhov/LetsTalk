"use client";

import { useLayoutEffect, useRef } from "react";
import { useMessages } from "@/features/chats/queries";
import { useChatStore } from "@/stores/chat-store";
import { formatTime } from "@/lib/format";
import styles from "./message-list.module.scss";

export function MessageList() {
    const chatId = useChatStore((s) => s.selectedChatId);
    const { data, isPending, error, hasNextPage, isFetchingNextPage, fetchNextPage } =
        useMessages(chatId);

    const scrollRef = useRef<HTMLDivElement>(null);
    const sentinelRef = useRef<HTMLDivElement>(null);
    const prevHeightRef = useRef(0);
    const prevCountRef = useRef(0);
    const prevChatRef = useRef<string | null>(null);

    const messages = data?.messages;

    // Initial load of a chat: jump to the bottom. After an older page is prepended:
    // keep the previously visible messages in place.
    useLayoutEffect(() => {
        const el = scrollRef.current;
        if (!el || !messages) return;
        if (prevChatRef.current !== chatId) {
            el.scrollTop = el.scrollHeight;
            prevChatRef.current = chatId;
        } else if (messages.length > prevCountRef.current && prevHeightRef.current) {
            el.scrollTop += el.scrollHeight - prevHeightRef.current;
        }
        prevHeightRef.current = el.scrollHeight;
        prevCountRef.current = messages.length;
    }, [chatId, messages]);

    useLayoutEffect(() => {
        const el = scrollRef.current;
        const sentinel = sentinelRef.current;
        if (!el || !sentinel || !hasNextPage) return;
        const observer = new IntersectionObserver(
            ([entry]) => {
                if (entry.isIntersecting && !isFetchingNextPage) {
                    prevHeightRef.current = el.scrollHeight;
                    void fetchNextPage();
                }
            },
            { root: el },
        );
        observer.observe(sentinel);
        return () => observer.disconnect();
    }, [hasNextPage, isFetchingNextPage, fetchNextPage, messages]);

    if (!chatId) return <p className={styles.hint}>Select a chat.</p>;
    if (isPending) return <p className={styles.hint}>Loading messages…</p>;
    if (error) return <p className={styles.hint}>Failed to load messages: {error.message}</p>;
    if (messages!.length === 0) return <p className={styles.hint}>No messages yet.</p>;

    return (
        <div ref={scrollRef} className={styles.scroll}>
            <div ref={sentinelRef} className={styles.sentinel}>
                {isFetchingNextPage
                    ? "Loading older messages…"
                    : hasNextPage
                      ? ""
                      : "Beginning of conversation"}
            </div>
            {messages!.map((m) => (
                <div key={m.id} className={`${styles.message} ${m.isMine ? styles.mine : ""}`}>
                    <div className={styles.text}>{m.text}</div>
                    <div className={styles.time}>{formatTime(m.created)}</div>
                </div>
            ))}
        </div>
    );
}
