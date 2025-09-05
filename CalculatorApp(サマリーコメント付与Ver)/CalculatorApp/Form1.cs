using System;
using System.Windows.Forms;

namespace CalculatorApp
{
	public partial class Form1 : Form
	{
		/// <summary>
		/// 最初の値を保持する変数
		/// </summary>
		private decimal firstValue = 0;
		
		/// <summary>
		/// 2番目の値を保持する変数
		/// </summary>
		private decimal secondValue = 0;
		
		/// <summary>
		/// テキストボックスの上書きモードを示すフラグ
		/// </summary>
		private bool text_overwrite = false;

		/// <summary>
		/// 小数点が入力されているかを示すフラグ</
		/// summary>
		private bool Num_Dot = false;

		/// <summary>
		/// 加算演算子の記号
		/// </summary>
		private const string AddSymbol = "+";

		/// <summary>
		/// 減算演算子の記号
		/// </summary>
		private const string SubtractSymbol = "-";

		/// <summary>
		/// 乗算演算子の記号
		/// </summary>
		private const string MultiplySymbol = "×";

		/// <summary>
		/// 除算演算子の記号
		/// </summary>
		private const string DivideSymbol = "÷";

		/// <summary>
		/// 等号演算子の記号
		/// </summary>
		private const string EqualSymbol = "=";

		/// <summary>
		/// 初期値：0
		/// </summary>
		private const decimal InitialValue = 0m;

		/// <summary>
		/// 表示値：0
		/// </summary>
		private const string ZeroValue = "0";

		/// <summary>
		/// パーセント値を小数に変換するための乗数
		/// </summary>
		private const decimal PercentMultiplier = 0.01m;

		/// <summary>
		/// 演算子の種類を定義する列挙型
		/// </summary>
		private enum OperatorType
		{
			/// <summary>演算なし</summary>
			NON,
			/// <summary>加算</summary>
			ADD,
			/// <summary>減算</summary>
			SUBTRACT,
			/// <summary>乗算</summary>
			MULTIPLY,
			/// <summary>除算</summary>
			DIVIDE,
			/// <summary>パーセント</summary>
			PERCENT
		}

		/// <summary>現在の演算子のタイプを保持する変数</summary>
		private OperatorType currentOperatorType = OperatorType.NON;

		/// <summary>
		/// フォームのコンストラクタ
		/// </summary>
		public Form1()
		{
			InitializeComponent();
		}

		/// <summary>
		/// フォームのロード時に実行されるイベントハンドラー
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void Form1_Load(object sender, EventArgs e)
		{
			textResult.Text = "0";
			text_overwrite = true;

			// 計算結果表示欄設定（読み取り専用、右揃え、ボーダースタイルなし）
			textResult.ReadOnly = true;
			textResult.TextAlign = HorizontalAlignment.Right;
			textResult.BorderStyle = BorderStyle.None;

			// 途中結果表示欄設定（読み取り専用、右揃え、ボーダースタイルなし）
			textExpression.ReadOnly = true;
			textExpression.TextAlign = HorizontalAlignment.Right;
			textExpression.BorderStyle = BorderStyle.None;
		}

		private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
		{

		}

		private void textResult_TextChanged(object sender, EventArgs e)
		{

		}

		private void textExpression_TextChanged(object sender, EventArgs e)
		{

		}

		/// <summary>
		/// 指定された演算子タイプに基づいて演算子の記号を取得する
		/// </summary>
		/// <param name="type">演算子のタイプ</param>
		/// <returns>指定された演算子タイプに対応する記号</returns>
		private string GetOperatorSymbol(OperatorType type)
		{
			string symbol = string.Empty;

			switch (type)
			{
				case OperatorType.ADD:
					return AddSymbol;
				case OperatorType.SUBTRACT:
					return SubtractSymbol;
				case OperatorType.MULTIPLY:
					return MultiplySymbol;
				case OperatorType.DIVIDE:
					return DivideSymbol;
				default:
					return string.Empty;
			}
		}

		/// <summary>
		/// 左辺と右辺の値を指定された演算子タイプに基づいて計算する
		/// </summary>
		/// <param name="left">左辺の値</param>
		/// <param name="right">右辺の値</param>
		/// <param name="type">演算子のタイプ</param>
		/// <returns>計算結果</returns>
		private decimal Calculate(decimal left, decimal right, OperatorType type)
		{
			switch (type)
			{
				case OperatorType.ADD:
					return left + right;
				case OperatorType.SUBTRACT:
					return left - right;
				case OperatorType.MULTIPLY:
					return left * right;
				case OperatorType.DIVIDE:
					return left / right;
				default:
					return right;
			}
		}

		/// <summary>
		/// 指定された演算子タイプに基づいて演算処理を実行する
		/// </summary>
		/// <param name="operationType">実行する演算子のタイプ</param>
		private void ProcessOperation(OperatorType operationType)
		{
			decimal currentValue = decimal.Parse(textResult.Text);

			if (textExpression.Text.EndsWith("=") || currentOperatorType == OperatorType.NON)
			{
				firstValue = currentValue;
			}
			else
			{
				UpdateResult(currentValue);
			}

			SetCurrentOperator(operationType);
			UpdateExpression();
		}

		/// <summary>
		/// 計算結果表示欄を更新する
		/// </summary>
		/// <param name="currentValue">現在の値</param>
		private void UpdateResult(decimal currentValue)
		{
			if (text_overwrite)
			{
				textResult.Text = firstValue.ToString("G29");
			}
			else
			{
				decimal result = Calculate(firstValue, currentValue, currentOperatorType);
				textResult.Text = result.ToString("G29");
				firstValue = result;
			}
		}

		/// <summary>
		/// 現在の演算子を設定する
		/// </summary>
		/// <param name="operationType">設定する演算子のタイプ</param>
		private void SetCurrentOperator(OperatorType operationType)
		{
			currentOperatorType = operationType;
			text_overwrite = true;
		}

		/// <summary>
		/// 途中計算表示欄を更新する
		/// </summary>
		private void UpdateExpression()
		{
			string operatorSymbol = GetOperatorSymbol(currentOperatorType);
			textExpression.Text = string.Format("{0} {1}", firstValue.ToString("G29"), operatorSymbol);
		}

		/// <summary>
		/// 演算ボタンがクリックされたときに実行されるイベントハンドラー
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnOperation_Click(object sender, EventArgs e)
		{
			Button btn = (Button) sender;
			OperatorType operationType = OperatorType.NON;

			switch (btn.Text)
			{
				case AddSymbol:
					operationType = OperatorType.ADD;
					break;
				case SubtractSymbol:
					operationType = OperatorType.SUBTRACT;
					break;
				case MultiplySymbol:
					operationType = OperatorType.MULTIPLY;
					break;
				case DivideSymbol:
					operationType = OperatorType.DIVIDE;
					break;
			}

			ProcessOperation(operationType);
		}

		/// <summary>
		/// 数字ボタンがクリックされたときに実行されるイベントハンドラー
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnNum_Click(object sender, EventArgs e)
		{
			Button btn = (Button)sender;

			// 直前の計算結果が表示されている場合、初期化する
			if (ShouldResetCalculator())
			{
				ResetCalculator();
			}

			// 入力欄が0または上書きモードの場合、新しい数値を設定
			if (IsInputFieldEmpty() || text_overwrite)
			{
				SetNewValue(btn.Text);
			}
			else
			{
				AppendValue(btn.Text);
			}
		}

		/// <summary>
		/// 計算をリセットする必要があるかどうかを判断する
		/// </summary>
		/// <returns>リセットが必要な場合は true、それ以外は false</returns>
		private bool ShouldResetCalculator()
		{
			return textExpression.Text.EndsWith(EqualSymbol);
		}

		/// <summary>
		/// 計算の状態をリセットする
		/// </summary>
		private void ResetCalculator()
		{
			textExpression.Text = string.Empty;
			firstValue = 0;
			currentOperatorType = OperatorType.NON;
			text_overwrite = true;
			Num_Dot = false;
		}

		/// <summary>
		/// 入力欄が空であるかどうかを判断する
		/// </summary>
		/// <returns>入力欄が0の場合は true、それ以外は false</returns>
		private bool IsInputFieldEmpty()
		{
			return textResult.Text == ZeroValue;
		}

		/// <summary>
		/// 新しい数値を設定する
		/// </summary>
		/// <param name="value">設定する数値</param>
		private void SetNewValue(string value)
		{
			textResult.Text = value;
			text_overwrite = false;
			Num_Dot = false;
		}

		/// <summary>
		/// 数値を入力欄に追加する
		/// </summary>
		/// <param name="value">追加する数値</param>
		private void AppendValue(string value)
		{
			// 0の後に0が続く場合は無視
			if (textResult.Text == ZeroValue && value == ZeroValue)
			{
				return;
			}
			textResult.Text += value;
		}

		/// <summary>
		/// 小数点ボタンがクリックされたときに実行されるイベントハンドラー
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnDot_Click(object sender, EventArgs e)
		{
			// 入力欄が0または上書きモードの場合、0.を設定
			if (IsInputFieldZeroOrOverwriting())
			{
				SetInitialDecimalValue();
				ResetCalculatorIfNeeded();
				return;
			}

			// 小数点がまだ入力されていない場合、追加する
			if (!Num_Dot)
			{
				AppendDecimalPoint();
			}
		}

		/// <summary>
		/// 入力欄が0または上書きモードであるかどうかを判断する
		/// </summary>
		/// <returns>条件に該当する場合は true、それ以外は false</returns>
		private bool IsInputFieldZeroOrOverwriting()
		{
			return textResult.Text == ZeroValue || text_overwrite;
		}

		/// <summary>
		/// 入力欄に初期値として0.を設定する
		/// </summary>
		private void SetInitialDecimalValue()
		{
			textResult.Text = "0.";
			text_overwrite = false;
			Num_Dot = true;
		}

		/// <summary>
		/// 直前の計算結果が表示されている場合、計算の状態を初期化する
		/// </summary>
		private void ResetCalculatorIfNeeded()
		{
			if (textExpression.Text.EndsWith(EqualSymbol))
			{
				textExpression.Text = string.Empty;
				firstValue = InitialValue;
				currentOperatorType = OperatorType.NON;
			}
		}

		/// <summary>
		/// 入力欄に小数点を追加する
		/// </summary>
		private void AppendDecimalPoint()
		{
			textResult.Text += ".";
			Num_Dot = true;
		}

		/// <summary>
		/// パーセントボタンがクリックされたときに実行されるイベントハンドラー
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnPercent_Click(object sender, EventArgs e)
		{
			// 演算タイプが非選択の場合、初期化する
			if (currentOperatorType == OperatorType.NON)
			{
				ResetDisplayToInitialValue();
				return;
			}

			decimal currentValue = decimal.Parse(textResult.Text);
			decimal percentValue = CalculatePercent(currentValue);
			decimal previousValue = firstValue;

			// 演算タイプに応じてパーセント計算を実行
			if (IsAdditionOrSubtraction())
			{
				PerformAdditionOrSubtraction(previousValue, percentValue);
			}
			else
			{
				PerformMultiplicationOrDivision(previousValue, percentValue);
			}

			text_overwrite = true;
			Num_Dot = false;
		}

		/// <summary>
		/// 表示を初期値にリセットする
		/// </summary>
		private void ResetDisplayToInitialValue()
		{
			textResult.Text = InitialValue.ToString();
			textExpression.Text = InitialValue.ToString();
		}

		/// <summary>
		/// 現在の値のパーセント値を計算する
		/// </summary>
		/// <param name="value">現在の値</param>
		/// <returns>計算されたパーセント値</returns>
		private decimal CalculatePercent(decimal value)
		{
			return value * PercentMultiplier;
		}

		/// <summary>
		/// 演算タイプが加算または減算かどうかを判断する
		/// </summary>
		/// <returns>加算または減算の場合は true、それ以外は false</returns>
		private bool IsAdditionOrSubtraction()
		{
			return currentOperatorType == OperatorType.ADD || currentOperatorType == OperatorType.SUBTRACT;
		}

		/// <summary>
		/// 加算または減算の演算を実行する
		/// </summary>
		/// <param name="previousValue">前の値</param>
		/// <param name="percentValue">計算されたパーセント値</param>
		private void PerformAdditionOrSubtraction(decimal previousValue, decimal percentValue)
		{
			string operatorSymbol = GetOperatorSymbol(currentOperatorType);
			decimal calculatedValue = previousValue * percentValue;

			textExpression.Text = string.Format("{0} {1} {2}", previousValue, operatorSymbol, calculatedValue.ToString("G29"));
			textResult.Text = calculatedValue.ToString("G29");
		}

		/// <summary>
		/// 乗算または除算の演算を実行する
		/// </summary>
		/// <param name="previousValue">前の値</param>
		/// <param name="percentValue">計算されたパーセント値</param>
		private void PerformMultiplicationOrDivision(decimal previousValue, decimal percentValue)
		{
			string operatorSymbol = GetOperatorSymbol(currentOperatorType);
			textExpression.Text = string.Format("{0} {1} {2}", previousValue, operatorSymbol, percentValue.ToString("G29"));
			textResult.Text = percentValue.ToString("G29");
		}

		/// <summary>
		/// イコールキーがクリックされたときに実行されるイベントハンドラー
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnEnter_Click(object sender, EventArgs e)
		{
			decimal currentValue = decimal.Parse(textResult.Text);

			if (currentOperatorType != OperatorType.NON)
			{
				HandleCalculation(currentValue);
			}
			else
			{
				DisplaySingleValue(currentValue);
			}

			text_overwrite = true;
			Num_Dot = textResult.Text.Contains(".");
		}

		/// <summary>
		/// 計算処理を行い、結果を表示する
		/// </summary>
		/// <param name="currentValue">現在の値</param>
		private void HandleCalculation(decimal currentValue)
		{
			bool isFirstEqual = !textExpression.Text.EndsWith(EqualSymbol);
			decimal result;
			string operatorSymbol = GetOperatorSymbol(currentOperatorType);

			if (isFirstEqual)
			{
				secondValue = currentValue;
				result = Calculate(firstValue, secondValue, currentOperatorType);
				UpdateExpression(firstValue, operatorSymbol, secondValue);
			}
			else
			{
				result = Calculate(currentValue, secondValue, currentOperatorType);
				UpdateExpression(currentValue, operatorSymbol, secondValue);
			}

			textResult.Text = result.ToString("G29");
			firstValue = result;
		}

		/// <summary>
		/// 単一の値を表示する
		/// </summary>
		/// <param name="currentValue">現在の値</param>
		private void DisplaySingleValue(decimal currentValue)
		{
			textExpression.Text = string.Format("{0} {1}", currentValue.ToString("G29"), EqualSymbol);
			textResult.Text = currentValue.ToString("G29");
		}

		/// <summary>
		/// 途中計算表示欄を更新する
		/// </summary>
		/// <param name="leftValue">左辺の値</param>
		/// <param name="operatorSymbol">演算子のシンボル</param>
		/// <param name="rightValue">右辺の値</param>
		private void UpdateExpression(decimal leftValue, string operatorSymbol, decimal rightValue)
		{
			textExpression.Text = string.Format("{0} {1} {2} {3}", leftValue.ToString("G29"), operatorSymbol, rightValue.ToString("G29"), EqualSymbol);
		}

		/// <summary>
		/// クリアエントリーキーがクリックされたときに実行されるイベントハンドラー
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnClearEntry_Click(object sender, EventArgs e)
		{

			textResult.Text = InitialValue.ToString();
			text_overwrite = true;
			Num_Dot = false;
		}

		/// <summary>
		/// クリアキーがクリックされたときに実行されるイベントハンドラー
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnClear_Click(object sender, EventArgs e)
		{

			firstValue = InitialValue;
			secondValue = InitialValue;
			textResult.Text = InitialValue.ToString();
			textExpression.Text = string.Empty;
			currentOperatorType = OperatorType.NON;
			text_overwrite = true;
			Num_Dot = false;
		}

		/// <summary>
		/// 桁下げキーがクリックされたときに実行されるイベントハンドラー
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnBack_Click(object sender, EventArgs e)
		{
			if (textResult.Text.Length > 0)
			{
				HandleBackspace();
			}
		}

		/// <summary>
		/// テキスト結果から最後の桁を削除する処理
		/// </summary>
		private void HandleBackspace()
		{
			if (textResult.Text == ZeroValue || text_overwrite)
			{
				text_overwrite = false;
			}

			if (textResult.Text.Length > 1)
			{
				RemoveLastCharacter();
			}
			else
			{
				ResetTextResult();
			}
		}

		/// <summary>
		/// 最後の文字を削除する処理
		/// </summary>
		private void RemoveLastCharacter()
		{
			textResult.Text = textResult.Text.Substring(0, textResult.Text.Length - 1);

			if (!textResult.Text.Contains("."))
			{
				Num_Dot = false;
			}
		}

		/// <summary>
		/// テキスト結果をリセットする処理
		/// </summary>
		private void ResetTextResult()
		{
			textResult.Text = InitialValue.ToString();
			Num_Dot = false;
			text_overwrite = true;
		}

		/// <summary>
		/// サインチェンジキーがクリックされたときに実行されるイベントハンドラー
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnTogglesign_Click(object sender, EventArgs e)
		{
			// テキスト結果が空でない場合、数値を反転させる
			if (!string.IsNullOrEmpty(textResult.Text))
			{
				decimal value;
				if (decimal.TryParse(textResult.Text, out value))
				{
					value = -value;
					textResult.Text = value.ToString();
				}
			}
		}

		/// <summary>
		/// 最前面表示キーがクリックされたときに実行されるイベントハンドラー
		/// </summary>
		/// <param name="sender">イベントの発生元</param>
		/// <param name="e">イベントデータ</param>
		private void btnTopMost_Click(object sender, EventArgs e)
		{
			// ウィンドウの最前面表示状態を切り替える
			this.TopMost = !this.TopMost;
		}
	}
}