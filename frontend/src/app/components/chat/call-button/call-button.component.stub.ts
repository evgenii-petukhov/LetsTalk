import { Component, EventEmitter, Input, Output } from '@angular/core';

@Component({
    selector: 'app-call-button',
    template: '<div></div>',
    standalone: false,
})
export class CallButtonStubComponent {
    @Input() mode: 'start-call' | 'end-call' = 'start-call';
    @Output() buttonClick = new EventEmitter();
}