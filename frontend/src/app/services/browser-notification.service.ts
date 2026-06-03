import { inject, Injectable } from '@angular/core';
import { ToastrService } from 'ngx-toastr';

@Injectable({
    providedIn: 'root',
})
export class BrowserNotificationService {
    private readonly toastr = inject(ToastrService);
    private isServiceWorkerRegistered = false;

    async init() {
        if (this.isServiceWorkerRegistered) {
            return;
        }

        try {
            await navigator.serviceWorker.register('notification_sw.js');
            this.isServiceWorkerRegistered = true;
            console.log('serviceWorker success');
        } catch {
            console.log('serviceWorker error');
        }
    }

    async showNotification(
        title: string,
        message: string,
        isWindowActive: boolean,
    ): Promise<void> {
        const permission = await Notification.requestPermission();
        if (permission === 'granted') {
            if (this.isServiceWorkerRegistered) {
                const registration = await navigator.serviceWorker.ready;
                registration.showNotification(title, {
                    body: message,
                });
            } else {
                new Notification(title, {
                    body: message,
                });
            }
        } else if (isWindowActive) {
            this.toastr.info(message, title);
        }
    }
}
