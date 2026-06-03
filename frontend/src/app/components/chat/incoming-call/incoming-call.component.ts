import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Store } from '@ngrx/store';
import { StoreService } from '../../../services/store.service';
import { selectCaller } from '../../../state/video-call/video-call.selectors';

@Component({
    selector: 'app-incoming-call',
    templateUrl: './incoming-call.component.html',
    styleUrl: './incoming-call.component.scss',
    standalone: false,
})
export class IncomingCallComponent {
    private readonly storeService = inject(StoreService);
    private readonly store = inject(Store);

    caller = toSignal(this.store.select(selectCaller));

    urlOptions = computed(() => {
        const caller = this.caller();

        return caller && [caller.image, caller.photoUrl];
    });

    callerName = computed(() => this.caller()?.chatName);

    onAcceptClicked(): void {
        this.storeService.acceptIncomingCall();
    }

    onDeclineClicked(): void {
        this.storeService.resetCall();
    }
}
