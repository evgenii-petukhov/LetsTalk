import { createActionGroup, props } from '@ngrx/store';
import { IAccountDto } from '../../api-client/api-client';

export const accountsActions = createActionGroup({
    source: 'Accounts',
    events: {
        init: props<{ accounts: readonly IAccountDto[] }>(),
    },
});
