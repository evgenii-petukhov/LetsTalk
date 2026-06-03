import { Component, inject, OnDestroy, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { faCirclePlus } from '@fortawesome/free-solid-svg-icons';
import { Subject, takeUntil } from 'rxjs';

@Component({
    selector: 'app-sidebar',
    templateUrl: './sidebar.component.html',
    styleUrls: ['./sidebar.component.scss'],
    standalone: false,
})
export class SidebarComponent implements OnDestroy {
    faCirclePlus = faCirclePlus;
    isChatListShown = signal(true);
    isAccountListShown = signal(false);

    private readonly activatedRoute = inject(ActivatedRoute);
    private readonly unsubscribe$: Subject<void> = new Subject<void>();

    constructor() {
        this.activatedRoute.queryParams
            .pipe(takeUntil(this.unsubscribe$))
            .subscribe((params) => {
                const showContacts = params['show_contacts'] === '1';
                this.isChatListShown.set(!showContacts);
                this.isAccountListShown.set(showContacts);
            });
    }

    ngOnDestroy(): void {
        this.unsubscribe$.next();
        this.unsubscribe$.complete();
    }
}
