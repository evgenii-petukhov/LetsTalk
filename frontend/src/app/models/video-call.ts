export interface VideoCall {
    callId?: string;
    chatId?: string;
    offer?: string;
    status: 'outgoing' | 'incoming-awaiting' | 'incoming-active';
    captureVideo: boolean;
    captureAudio: boolean;
    isMinimized: boolean;
    facingMode: 'user' | 'environment';
}