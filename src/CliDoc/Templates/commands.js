// CliDoc Application
class CliDocApp {
    constructor(data) {
        this.data = data;
        this.commands = new Map();
        this.selectedCommandId = null;
        this.currentView = 'column';
        this.searchTerm = '';
        
        this.init();
    }

    init() {
        // Index commands by ID
        this.data.commands.forEach(cmd => {
            this.commands.set(cmd.id, cmd);
        });

        // Set up event listeners
        this.setupThemeToggle();
        this.setupViewToggle();
        this.setupSearch();

        // Render initial view
        this.renderBrowser();

        // Select root command by default
        const rootCommand = this.data.commands.find(cmd => cmd.isRoot);
        if (rootCommand) {
            this.selectCommand(rootCommand.id);
        }
    }

    setupThemeToggle() {
        // Load saved theme
        const currentTheme = localStorage.getItem('theme') || 'light';
        document.documentElement.setAttribute('data-theme', currentTheme);
        document.body.setAttribute('data-theme', currentTheme);

        // Setup sidebar theme toggle (if exists)
        const sidebarToggle = document.getElementById('theme-toggle');
        if (sidebarToggle) {
            sidebarToggle.addEventListener('click', () => this.toggleTheme());
        }

        // Setup top nav theme toggle (if exists)
        const topNavToggle = document.querySelector('.theme-toggle-btn');
        if (topNavToggle) {
            topNavToggle.addEventListener('click', () => this.toggleTheme());
        }
    }

    toggleTheme() {
        const newTheme = document.documentElement.getAttribute('data-theme') === 'light' ? 'dark' : 'light';
        document.documentElement.setAttribute('data-theme', newTheme);
        document.body.setAttribute('data-theme', newTheme);
        localStorage.setItem('theme', newTheme);
    }

    setupViewToggle() {
        const viewButtons = document.querySelectorAll('.view-btn');
        viewButtons.forEach(btn => {
            btn.addEventListener('click', () => {
                viewButtons.forEach(b => b.classList.remove('active'));
                btn.classList.add('active');
                this.currentView = btn.dataset.view;
                this.renderBrowser();
            });
        });
    }

    setupSearch() {
        const searchInput = document.getElementById('search-input');
        searchInput.addEventListener('input', (e) => {
            this.searchTerm = e.target.value.toLowerCase();
            this.renderBrowser();
        });
    }

    renderBrowser() {
        const browser = document.getElementById('command-browser');
        browser.innerHTML = '';

        const filteredCommands = this.getFilteredCommands();

        if (this.currentView === 'column') {
            browser.appendChild(this.renderColumnView(filteredCommands));
        } else if (this.currentView === 'tree') {
            browser.appendChild(this.renderTreeView(filteredCommands));
        } else {
            browser.appendChild(this.renderListView(filteredCommands));
        }
    }

    getFilteredCommands() {
        if (!this.searchTerm) {
            return this.data.commands;
        }

        return this.data.commands.filter(cmd => 
            cmd.name.toLowerCase().includes(this.searchTerm) ||
            cmd.fullName.toLowerCase().includes(this.searchTerm) ||
            cmd.description.toLowerCase().includes(this.searchTerm)
        );
    }

    renderColumnView(commands) {
        const container = document.createElement('div');
        container.className = 'column-view';

        // Group commands by depth
        const maxDepth = Math.max(...commands.map(c => c.depth));
        
        for (let depth = 0; depth <= maxDepth; depth++) {
            const column = document.createElement('div');
            column.className = 'command-column';

            const title = document.createElement('div');
            title.className = 'column-title';
            title.textContent = depth === 0 ? 'Root' : `Level ${depth}`;
            column.appendChild(title);

            const commandsAtDepth = commands.filter(c => c.depth === depth);
            commandsAtDepth.forEach(cmd => {
                const pill = this.createCommandPill(cmd);
                column.appendChild(pill);
            });

            if (commandsAtDepth.length > 0) {
                container.appendChild(column);
            }
        }

        return container;
    }

    renderTreeView(commands) {
        const container = document.createElement('div');
        container.className = 'tree-view';

        const rootCommands = commands.filter(c => c.isRoot);
        rootCommands.forEach(cmd => {
            container.appendChild(this.renderTreeNode(cmd, commands, 0));
        });

        return container;
    }

    renderTreeNode(command, allCommands, depth) {
        const node = document.createElement('div');
        node.style.marginLeft = `${depth * 1.5}rem`;

        const item = document.createElement('div');
        item.className = 'tree-item';
        if (this.selectedCommandId === command.id) {
            item.classList.add('selected');
        }

        // Add toggle for groups
        if (command.isGroup) {
            const toggle = document.createElement('button');
            toggle.className = 'tree-toggle expanded';
            toggle.innerHTML = '▸';
            toggle.addEventListener('click', (e) => {
                e.stopPropagation();
                toggle.classList.toggle('expanded');
                const children = node.querySelector('.tree-children');
                if (children) {
                    children.classList.toggle('expanded');
                }
            });
            item.appendChild(toggle);
        }

        const name = document.createElement('span');
        name.textContent = command.name;
        item.appendChild(name);

        item.addEventListener('click', () => this.selectCommand(command.id));
        node.appendChild(item);

        // Add children
        if (command.isGroup) {
            const childrenContainer = document.createElement('div');
            childrenContainer.className = 'tree-children expanded';

            command.children.forEach(childId => {
                const childCmd = allCommands.find(c => c.id === childId);
                if (childCmd) {
                    childrenContainer.appendChild(this.renderTreeNode(childCmd, allCommands, depth + 1));
                }
            });

            node.appendChild(childrenContainer);
        }

        return node;
    }

    renderListView(commands) {
        const container = document.createElement('div');
        container.className = 'list-view';

        commands.forEach(cmd => {
            const item = document.createElement('div');
            item.className = 'list-item';
            if (this.selectedCommandId === cmd.id) {
                item.classList.add('selected');
            }

            const name = document.createElement('div');
            name.className = 'list-item-name';
            name.textContent = cmd.fullName;
            item.appendChild(name);

            if (cmd.description) {
                const desc = document.createElement('div');
                desc.className = 'list-item-desc';
                desc.textContent = cmd.description;
                item.appendChild(desc);
            }

            item.addEventListener('click', () => this.selectCommand(cmd.id));
            container.appendChild(item);
        });

        return container;
    }

    createCommandPill(command) {
        const pill = document.createElement('div');
        pill.className = 'command-pill';
        if (command.isGroup) {
            pill.classList.add('group');
        }
        if (this.selectedCommandId === command.id) {
            pill.classList.add('selected');
        }

        pill.textContent = command.name;
        pill.addEventListener('click', () => this.selectCommand(command.id));

        return pill;
    }

    selectCommand(commandId) {
        this.selectedCommandId = commandId;
        this.renderBrowser();
        this.renderDetail(commandId);
    }

    renderDetail(commandId) {
        const command = this.commands.get(commandId);
        if (!command) return;

        const detailContainer = document.getElementById('command-detail');
        detailContainer.innerHTML = '';

        // Breadcrumb
        const breadcrumb = this.createBreadcrumb(command);
        detailContainer.appendChild(breadcrumb);

        // Title
        const title = document.createElement('h1');
        title.className = 'command-title';
        title.textContent = command.fullName;
        detailContainer.appendChild(title);

        // Description
        if (command.description) {
            const desc = document.createElement('p');
            desc.className = 'command-description';
            desc.textContent = command.description;
            detailContainer.appendChild(desc);
        }

        // Arguments
        if (command.arguments && command.arguments.length > 0) {
            detailContainer.appendChild(this.renderArgumentsTable(command.arguments));
        }

        // Options
        if (command.options && command.options.length > 0) {
            detailContainer.appendChild(this.renderOptionsTable(command.options));
        }

        // Examples
        if (command.examples && command.examples.length > 0) {
            detailContainer.appendChild(this.renderExamples(command.examples));
        }

        // Sections (only for non-root commands; root sections go on the home page)
        if (command.sections && command.sections.length > 0 && !command.isRoot) {
            command.sections.forEach(section => {
                detailContainer.appendChild(this.renderSection(section));
            });
        }

        // Subcommands
        if (command.isGroup && command.children.length > 0) {
            detailContainer.appendChild(this.renderSubcommands(command.children));
        }
    }

    createBreadcrumb(command) {
        const breadcrumb = document.createElement('div');
        breadcrumb.className = 'breadcrumb';

        const parts = command.fullName.split(' ');
        parts.forEach((part, index) => {
            if (index > 0) {
                const separator = document.createElement('span');
                separator.className = 'breadcrumb-separator';
                separator.textContent = '›';
                breadcrumb.appendChild(separator);
            }

            const item = document.createElement('span');
            item.className = 'breadcrumb-item';
            if (index === parts.length - 1) {
                item.classList.add('current');
            }
            item.textContent = part;
            breadcrumb.appendChild(item);
        });

        return breadcrumb;
    }

    renderArgumentsTable(args) {
        const section = document.createElement('div');
        section.className = 'section';

        const title = document.createElement('h2');
        title.className = 'section-title';
        title.textContent = 'Arguments';
        section.appendChild(title);

        const table = document.createElement('table');
        table.className = 'options-table';

        const thead = document.createElement('thead');
        thead.innerHTML = `
            <tr>
                <th>Name</th>
                <th>Description</th>
                <th>Required</th>
            </tr>
        `;
        table.appendChild(thead);

        const tbody = document.createElement('tbody');
        args.forEach(arg => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td><span class="option-name">${this.escapeHtml(arg.name)}</span></td>
                <td>${this.escapeHtml(arg.description)}</td>
                <td>${arg.isRequired ? '<span class="badge badge-required">Required</span>' : ''}</td>
            `;
            tbody.appendChild(row);
        });
        table.appendChild(tbody);

        section.appendChild(table);
        return section;
    }

    renderOptionsTable(options) {
        const section = document.createElement('div');
        section.className = 'section';

        const title = document.createElement('h2');
        title.className = 'section-title';
        title.textContent = 'Options';
        section.appendChild(title);

        const table = document.createElement('table');
        table.className = 'options-table';

        const thead = document.createElement('thead');
        thead.innerHTML = `
            <tr>
                <th>Option</th>
                <th>Type</th>
                <th>Description</th>
                <th>Default</th>
            </tr>
        `;
        table.appendChild(thead);

        const tbody = document.createElement('tbody');
        options.forEach(opt => {
            const row = document.createElement('tr');
            
            const optionCell = document.createElement('td');
            optionCell.innerHTML = `<span class="option-name">${this.escapeHtml(opt.name)}</span>`;
            if (opt.shortName) {
                optionCell.innerHTML += `<span class="option-alias">${this.escapeHtml(opt.shortName)}</span>`;
            }
            if (opt.isRequired) {
                optionCell.innerHTML += ' <span class="badge badge-required">Required</span>';
            }
            row.appendChild(optionCell);

            const typeCell = document.createElement('td');
            typeCell.innerHTML = `<span class="badge badge-type">${this.escapeHtml(opt.valueType)}</span>`;
            row.appendChild(typeCell);

            const descCell = document.createElement('td');
            descCell.textContent = opt.description;
            row.appendChild(descCell);

            const defaultCell = document.createElement('td');
            if (opt.defaultValue) {
                defaultCell.innerHTML = `<span class="default-value">${this.escapeHtml(opt.defaultValue)}</span>`;
            } else {
                defaultCell.textContent = '-';
            }
            row.appendChild(defaultCell);

            tbody.appendChild(row);
        });
        table.appendChild(tbody);

        section.appendChild(table);
        return section;
    }

    renderExamples(examples) {
        const section = document.createElement('div');
        section.className = 'section';

        const title = document.createElement('h2');
        title.className = 'section-title';
        title.textContent = 'Examples';
        section.appendChild(title);

        const list = document.createElement('div');
        list.className = 'examples-list';

        examples.forEach(example => {
            const exampleDiv = document.createElement('div');
            exampleDiv.className = 'example';

            if (example.description) {
                const desc = document.createElement('div');
                desc.className = 'example-desc';
                desc.textContent = example.description;
                exampleDiv.appendChild(desc);
            }

            const codeWrapper = document.createElement('div');
            codeWrapper.className = 'example-code-wrapper';

            const code = document.createElement('pre');
            code.className = 'example-code';
            code.textContent = example.command;
            codeWrapper.appendChild(code);

            const copyBtn = document.createElement('button');
            copyBtn.className = 'copy-btn';
            copyBtn.innerHTML = `
                <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10 8V7C10 6.05719 10 5.58579 10.2929 5.29289C10.5858 5 11.0572 5 12 5H17C17.9428 5 18.4142 5 18.7071 5.29289C19 5.58579 19 6.05719 19 7V12C19 12.9428 19 13.4142 18.7071 13.7071C18.4142 14 17.9428 14 17 14H16M7 19H12C12.9428 19 13.4142 19 13.7071 18.7071C14 18.4142 14 17.9428 14 17V12C14 11.0572 14 10.5858 13.7071 10.2929C13.4142 10 12.9428 10 12 10H7C6.05719 10 5.58579 10 5.29289 10.2929C5 10.5858 5 11.0572 5 12V17C5 17.9428 5 18.4142 5.29289 18.7071C5.58579 19 6.05719 19 7 19Z"/></svg>
            `;
            copyBtn.addEventListener('click', () => this.copyToClipboard(example.command, copyBtn));
            codeWrapper.appendChild(copyBtn);

            exampleDiv.appendChild(codeWrapper);
            list.appendChild(exampleDiv);
        });

        section.appendChild(list);
        return section;
    }

    renderSection(section) {
        const sectionDiv = document.createElement('div');
        sectionDiv.className = 'section';

        const title = document.createElement('h2');
        title.className = 'section-title';
        title.textContent = section.title;
        sectionDiv.appendChild(title);

        const content = document.createElement('div');
        content.className = 'markdown-content';
        content.innerHTML = this.renderMarkdown(section.body);
        sectionDiv.appendChild(content);

        return sectionDiv;
    }

    renderSubcommands(childIds) {
        const section = document.createElement('div');
        section.className = 'section';

        const title = document.createElement('h2');
        title.className = 'section-title';
        title.textContent = 'Subcommands';
        section.appendChild(title);

        const list = document.createElement('div');
        list.className = 'subcommands-list';

        childIds.forEach(childId => {
            const child = this.commands.get(childId);
            if (!child) return;

            const card = document.createElement('div');
            card.className = 'subcommand-card';

            const name = document.createElement('div');
            name.className = 'subcommand-name';
            name.textContent = child.name;
            card.appendChild(name);

            if (child.description) {
                const desc = document.createElement('div');
                desc.className = 'subcommand-desc';
                desc.textContent = child.description;
                card.appendChild(desc);
            }

            card.addEventListener('click', () => this.selectCommand(childId));
            list.appendChild(card);
        });

        section.appendChild(list);
        return section;
    }

    copyToClipboard(text, button) {
        navigator.clipboard.writeText(text).then(() => {
            button.classList.add('copied');
            setTimeout(() => button.classList.remove('copied'), 2000);
        }).catch(err => {
            console.error('Failed to copy:', err);
        });
    }

    renderMarkdown(text) {
        // Simple markdown rendering (you could use a library like marked.js for more features)
        let html = text;

        // Code blocks
        html = html.replace(/```(\w+)?\n([\s\S]*?)```/g, '<pre><code>$2</code></pre>');

        // Inline code
        html = html.replace(/`([^`]+)`/g, '<code>$1</code>');

        // Bold
        html = html.replace(/\*\*([^*]+)\*\*/g, '<strong>$1</strong>');

        // Links
        html = html.replace(/\[([^\]]+)\]\(([^)]+)\)/g, '<a href="$2">$1</a>');

        // Paragraphs
        html = html.split('\n\n').map(p => `<p>${p.replace(/\n/g, '<br>')}</p>`).join('');

        return html;
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }
}
