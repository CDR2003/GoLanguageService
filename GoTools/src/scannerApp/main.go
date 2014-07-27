package main

import "fmt"
import "go/scanner"
import "go/token"
import "encoding/json"
import "os"
import "io/ioutil"

type GoToken struct {
	ID         int
	Type       string
	StartIndex int
	Text       string
}

func main() {
	bytes, _ := ioutil.ReadAll(os.Stdin)

	var s scanner.Scanner
	fileSet := token.NewFileSet()
	file := fileSet.AddFile("", fileSet.Base(), len(bytes))
	s.Init(file, bytes, nil, scanner.ScanComments)

	tokens := make([]*GoToken, 0)

	for {
		pos, tokenID, lit := s.Scan()
		if tokenID == token.EOF {
			break
		}

		tok := new(GoToken)
		tok.ID = int(tokenID)
		tok.StartIndex = int(pos) - 1
		tok.Text = lit

		if tokenID.IsKeyword() {
			tok.Type = "Keyword"
		} else if tokenID == token.SEMICOLON {
			tok.Type = "Delimiter"
		} else if tokenID == token.STRING || tokenID == token.CHAR {
			tok.Type = "String"
		} else if tokenID.IsLiteral() {
			tok.Type = "Literal"
		} else if tokenID.IsOperator() {
			tok.Type = "Operator"
		} else if tokenID == token.COMMENT {
			tok.Type = "Comment"
		} else {
			tok.Type = "Unknown"
		}

		tokens = append(tokens, tok)
	}

	result, _ := json.MarshalIndent(tokens, "", "    ")
	fmt.Println(string(result))
}
