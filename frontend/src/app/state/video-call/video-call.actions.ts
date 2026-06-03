import { createActionGroup, emptyProps, props } from '@ngrx/store';

export const videoCallActions = createActionGroup({
    source: 'videoCall',
    events: {
        initOutgoingCall: props<{ chatId: string }>(),
        initIncomingCall: props<{ callId: string; chatId: string; offer: string }>(),
        acceptIncomingCall: emptyProps(),
        setCallId: props<{ callId: string; }>(),
        toggleCaptureVideo: emptyProps(),
        toggleCaptureAudio: emptyProps(),
        reset: emptyProps(),
        minimize: emptyProps(),
        maximize: emptyProps(),
        toggleFacingMode: emptyProps(),
    },
});
