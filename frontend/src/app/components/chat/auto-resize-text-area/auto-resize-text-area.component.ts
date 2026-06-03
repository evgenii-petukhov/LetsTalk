import { Component, ElementRef, EventEmitter, Input, Output, ViewChild } from '@angular/core';

@Component({
    selector: 'app-auto-resize-text-area',
    templateUrl: './auto-resize-text-area.component.html',
    styleUrl: './auto-resize-text-area.component.scss',
    standalone: false,
})
export class AutoResizeTextAreaComponent {
    @Input() text = '';
    @Output() textChange = new EventEmitter<string>();

    @ViewChild('textarea')
    textareaRef: ElementRef<HTMLTextAreaElement>;

    @Output() submitted = new EventEmitter<string>();

    focus(): void {
        const element = this.textareaRef.nativeElement;
        element.style.height = 'auto';
        element.focus();
    }

    onTextChanged(): void {
        const element = this.textareaRef.nativeElement;
        element.style.height = 'auto';
        element.style.height = `${element.scrollHeight}px`;
        this.textChange.emit(this.text);
    }

    submit(): void {
        this.submitted.emit(this.text);
    }
}
