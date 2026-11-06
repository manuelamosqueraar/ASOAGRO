const fs = require('fs');

const files = process.argv.slice(2);
files.forEach(file => {
    const content = fs.readFileSync(file, 'utf8');
    let depth = 0;
    let unclosedComment = false;
    for (let i = 0; i < content.length; i++) {
        if (!unclosedComment && content[i] === '/' && content[i+1] === '*') {
            unclosedComment = true;
            i++;
        } else if (unclosedComment && content[i] === '*' && content[i+1] === '/') {
            unclosedComment = false;
            i++;
        } else if (!unclosedComment) {
            if (content[i] === '{') depth++;
            else if (content[i] === '}') depth--;
        }
    }
    console.log(file, 'depth:', depth, 'unclosedComment:', unclosedComment);
});

