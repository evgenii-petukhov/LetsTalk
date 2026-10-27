import { createActionGroup, emptyProps } from '@ngrx/store';

export const sessionActions = createActionGroup({
    source: 'Session',
    events: {
        ended: emptyProps(),
    },
});
